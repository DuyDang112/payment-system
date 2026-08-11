using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PaymentProcessing.Domain.Events;
using PaymentProcessing.Domain.Models;
using PaymentProcessing.Features.Shared.Clients;
using PaymentProcessing.Features.Shared.Errors;
using PaymentProcessing.Infrastructure.Data;
using PaymentProcessing.Infrastructure.Events;
using PaymentProcessing.Shared;
using Serilog;
using Shared;
using Shared.Domains;
using Shared.Observability;

namespace PaymentProcessing.Features.CreatePayment;

/// <summary>
/// Handler for creating payments with synchronous coordination
/// </summary>
internal sealed class CreatePaymentHandler(
    PaymentsDbContext context,
    IEventPublisher eventPublisher,
    RiskAssessmentClient riskAssessmentClient,
    PaymentRouterClient paymentRouterClient,
    BankAdapterClient bankAdapterClient,
    ILogger<CreatePaymentHandler> logger) : ICreatePaymentHandler
{
    public async Task<Result<CreatePaymentResponse>> HandleAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // Check for existing idempotency key
        var existingIdempotencyKey = await context.IdempotencyKeys
            .FirstOrDefaultAsync(
                ik => ik.MerchantId == request.MerchantId && ik.Key == request.IdempotencyKey,
                cancellationToken);

        if (existingIdempotencyKey != null)
        {
            if (existingIdempotencyKey.IsExpired())
            {
                logger.LogInformation(
                    "Idempotency key {Key} for merchant {MerchantId} has expired",
                    request.IdempotencyKey,
                    request.MerchantId);
                return PaymentErrors.IdempotencyKeyExpired;
            }

            // Return existing payment
            var existingPayment = await context.Payments
                .FirstOrDefaultAsync(p => p.Id == existingIdempotencyKey.PaymentId, cancellationToken);

            if (existingPayment != null)
            {
                logger.LogInformation(
                    "Returning existing payment {PaymentId} for idempotency key {Key}",
                    existingPayment.PaymentId,
                    request.IdempotencyKey);
                return existingPayment.MapToCreateResponse();
            }

            logger.LogWarning(
                "Idempotency key {Key} exists but payment not found",
                request.IdempotencyKey);
            return PaymentErrors.IdempotencyKeyConflict;
        }

        // Validate amount and currency
        if (request.Amount <= 0)
        {
            return PaymentErrors.InvalidAmount;
        }

        var currency = request.Currency.ToUpperInvariant();
        if (!IsSupportedCurrency(currency))
        {
            logger.LogInformation("Currency {Currency} is not supported", currency);
            return PaymentErrors.InvalidCurrency;
        }

        // Create payment
        var paymentId = GeneratePaymentId();
        var money = Money.Of(request.Amount, currency);
        var payment = Payment.Create(
            paymentId,
            request.MerchantId,
            request.CustomerId,
            money,
            request.IdempotencyKey,
            request.PaymentMethodToken);

        // Save to database
        await context.Payments.AddAsync(payment, cancellationToken);

        // Create idempotency key
        var idempotencyKey = IdempotencyKey.Create(
            request.IdempotencyKey,
            request.MerchantId,
            payment.Id);
        await context.IdempotencyKeys.AddAsync(idempotencyKey, cancellationToken);

        logger.LogInformation(
            "Created payment {PaymentId} for merchant {MerchantId}",
            paymentId,
            request.MerchantId);

        // Publish domain events
        await eventPublisher.PublishDomainEventsAsync(payment.DomainEvents, cancellationToken);
        payment.ClearDomainEvents();

        // Step 2: Synchronous Risk Assessment
        logger.LogInformation("Starting risk evaluation for payment {PaymentId}", paymentId);
        var riskResult = await riskAssessmentClient.EvaluateRiskAsync(new EvaluateRiskRequest
        {
            PaymentId = paymentId,
            MerchantId = request.MerchantId,
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            Currency = currency,
            CountryCode = request.Metadata?.GetValueOrDefault("countryCode"),
            IpAddress = request.Metadata?.GetValueOrDefault("customerIp"),
            CustomerEmail = request.Metadata?.GetValueOrDefault("customerEmail")
        }, cancellationToken);

        // Check if risk evaluation was successful
        var (riskSuccess, riskResponse, riskError) = riskResult;
        if (!riskSuccess || riskResponse == null)
        {
            logger.LogWarning("Risk evaluation failed for payment {PaymentId}: {Error}", paymentId, riskError);
            payment.MarkAsFailed("Risk assessment failed");
            payment.TransitionTo(PaymentStatus.Failed);
            await context.SaveChangesAsync(cancellationToken);
            return PaymentErrors.RiskAssessmentFailed;
        }

        if (riskResponse.Decision != "APPROVE")
        {
            logger.LogWarning("Payment {PaymentId} rejected by risk assessment: {Decision}", paymentId, riskResponse.Decision);
            payment.MarkAsFailed($"Risk assessment: {riskResponse.Decision}");
            payment.TransitionTo(PaymentStatus.Failed);
            await context.SaveChangesAsync(cancellationToken);

            // Record metrics
            stopwatch.Stop();
            MetricHelper.RecordPaymentCompletion(
                status: "failed",
                currency: currency,
                provider: "unknown",
                riskDecision: "rejected",
                durationSeconds: stopwatch.Elapsed.TotalSeconds
            );

            return PaymentErrors.RiskAssessmentRejected;
        }

        logger.LogInformation("Payment {PaymentId} approved by risk assessment (Score: {Score})", paymentId, riskResponse.RiskScore);

        // Step 2B: Synchronous Payment Routing
        logger.LogInformation("Starting payment routing for payment {PaymentId}", paymentId);

        // Validate payment method
        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethodEnum))
        {
            logger.LogWarning("Invalid payment method {PaymentMethod} for payment {PaymentId}", request.PaymentMethod, paymentId);
            return PaymentErrors.InvalidPaymentMethod;
        }

        var (routeSuccess, routeResponse, routeError) = await paymentRouterClient.RoutePaymentAsync(new RoutePaymentRequest
        {
            PaymentId = paymentId,
            MerchantId = request.MerchantId,
            Amount = request.Amount,
            Currency = currency,
            PaymentMethod = paymentMethodEnum.ToString().ToUpper(), // Send enum name in uppercase (CREDIT_CARD)
            CountryCode = request.Metadata?.GetValueOrDefault("countryCode"),
            Strategy = "COST_BASED"
        }, cancellationToken);

        if (!routeSuccess || routeResponse == null)
        {
            logger.LogError("Payment routing failed for payment {PaymentId}: {Error}", paymentId, routeError);
            payment.MarkAsFailed("Payment routing failed");
            payment.TransitionTo(PaymentStatus.Failed);
            await context.SaveChangesAsync(cancellationToken);

            // Record metrics
            stopwatch.Stop();
            MetricHelper.RecordPaymentCompletion(
                status: "failed",
                currency: currency,
                provider: "unknown",
                riskDecision: "approved",
                durationSeconds: stopwatch.Elapsed.TotalSeconds
            );

            return PaymentErrors.PaymentRoutingFailed;
        }

        logger.LogInformation("Payment {PaymentId} routed to provider {Provider}", paymentId, routeResponse.SelectedProviderId);

        // Step 3: Update payment status and proceed to authorization
        payment.TransitionTo(PaymentStatus.Processing);
        payment.TransitionTo(PaymentStatus.RiskEvaluation);
        payment.TransitionTo(PaymentStatus.Routing);
        payment.TransitionTo(PaymentStatus.Authorizing);

        // Step 4: Synchronous Bank Authorization
        logger.LogInformation("Starting bank authorization for payment {PaymentId} with provider {Provider}",
            paymentId, routeResponse.SelectedProviderId);

        var (authSuccess, authResponse, authError) = await bankAdapterClient.AuthorizeAsync(new AuthorizeRequest
        {
            PaymentId = paymentId,
            ProviderId = routeResponse.SelectedProviderId ?? "stripe",
            Amount = request.Amount,
            Currency = currency,
            Metadata = new Dictionary<string, string>
            {
                { "customerId", request.CustomerId },
                { "paymentMethodToken", request.PaymentMethodToken ?? "" },
                { "orderId", request.Metadata?.GetValueOrDefault("orderId", "") ?? "" }
            }
        }, cancellationToken);

        if (!authSuccess || authResponse == null)
        {
            logger.LogWarning("Bank authorization failed for payment {PaymentId}: {Error}", paymentId, authError);
            payment.MarkAsFailed("Bank authorization failed");
            payment.TransitionTo(PaymentStatus.Failed);
            payment.RecordAttempt(routeResponse.SelectedProviderId ?? "stripe", Domain.Models.AttemptStatus.Failed, "Authorization failed");
            await context.SaveChangesAsync(cancellationToken);

            // Record metrics
            stopwatch.Stop();
            MetricHelper.RecordPaymentCompletion(
                status: "failed",
                currency: currency,
                provider: routeResponse.SelectedProviderId ?? "stripe",
                riskDecision: "approved",
                durationSeconds: stopwatch.Elapsed.TotalSeconds
            );

            return PaymentErrors.BankAuthorizationFailed;
        }

        logger.LogInformation("Bank authorization succeeded for payment {PaymentId}: TransactionId={TransactionId}",
            paymentId, authResponse.ProviderTransactionId);

        // Step 5: Complete payment
        payment.RecordAttempt(routeResponse.SelectedProviderId ?? "stripe", Domain.Models.AttemptStatus.Completed);
        payment.MarkAsCompleted();
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Payment {PaymentId} completed successfully", paymentId);

        // Record metrics
        stopwatch.Stop();
        MetricHelper.RecordPaymentCompletion(
            status: "success",
            currency: currency,
            provider: routeResponse.SelectedProviderId ?? "stripe",
            riskDecision: "approved",
            durationSeconds: stopwatch.Elapsed.TotalSeconds
        );

        return payment.MapToCreateResponse();
    }

    private static bool IsSupportedCurrency(string currency)
    {
        var supported = new[] { "USD", "EUR", "GBP", "CAD", "AUD" };
        return supported.Contains(currency);
    }

    private static string GeneratePaymentId()
    {
        return $"pay_{Guid.NewGuid():N}";
    }
}
