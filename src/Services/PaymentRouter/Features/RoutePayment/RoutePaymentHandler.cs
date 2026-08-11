using Microsoft.EntityFrameworkCore;
using PaymentRouter.Domain.Events;
using PaymentRouter.Domain.Models;
using PaymentRouter.Features.Shared.Errors;
using PaymentRouter.Infrastructure.Data;
using PaymentRouter.Infrastructure.CircuitBreaking;
using PaymentRouter.Infrastructure.RoutingEngine;
using PaymentRouter.Shared;
using Shared;
using Shared.Observability;
using System.Diagnostics;

namespace PaymentRouter.Features.RoutePayment;

internal sealed class RoutePaymentHandler(
    PaymentRouterDbContext context,
    IRoutingEngine routingEngine,
    ICircuitBreakerManager circuitBreakerManager,
    IEventPublisher eventPublisher,
    ILogger<RoutePaymentHandler> logger) : IRoutePaymentHandler
{
    public async Task<Result<RoutePaymentResponse>> HandleAsync(
        RoutePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // Get all enabled providers
        var providers = await context.PaymentProviders
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Priority)
            .ToListAsync(cancellationToken);

        if (!providers.Any())
        {
            logger.LogWarning("No enabled providers found for routing");

            // Record routing failure metrics
            stopwatch.Stop();
            MetricHelper.RecordRoutingDecision(
                strategy: request.Strategy.ToString().ToLowerInvariant(),
                decisionReason: "no_enabled_providers",
                eligibleProviderCount: 0,
                durationSeconds: stopwatch.Elapsed.TotalSeconds
            );

            return Result<RoutePaymentResponse>.Failure(RoutingErrors.NoHealthyProviders);
        }

        // Filter providers by constraints
        var amount = new Money(request.Amount, request.Currency);
        var eligibleProviders = providers
            .Where(p => p.SupportsCurrency(request.Currency))
            .Where(p => p.SupportsMethod(request.PaymentMethod))
            .Where(p => p.IsWithinLimits(request.Amount))
            .Where(p => circuitBreakerManager.IsProviderAvailable(p.ProviderId))
            .ToList();

        if (!eligibleProviders.Any())
        {
            logger.LogWarning(
                "No eligible providers found for payment {PaymentId}. Currency: {Currency}, Method: {Method}",
                request.PaymentId,
                request.Currency,
                request.PaymentMethod);

            // Record routing failure metrics
            stopwatch.Stop();
            MetricHelper.RecordRoutingDecision(
                strategy: request.Strategy.ToString().ToLowerInvariant(),
                decisionReason: "no_eligible_providers",
                eligibleProviderCount: 0,
                durationSeconds: stopwatch.Elapsed.TotalSeconds
            );

            return Result<RoutePaymentResponse>.Failure(RoutingErrors.NoHealthyProviders);
        }

        // Create routing context
        var routingContext = new RoutingContext(
            request.MerchantId,
            request.PaymentId,
            amount,
            request.Currency,
            request.PaymentMethod,
            request.CountryCode);

        // Make routing decision
        try
        {
            var decision = await routingEngine.MakeRoutingDecisionAsync(
                routingContext,
                request.Strategy,
                cancellationToken);

            // Save decision to database
            await context.RoutingDecisions.AddAsync(decision, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            // Get selected provider details
            var selectedProvider = providers.First(p => p.ProviderId == decision.SelectedProviderId);

            var response = new RoutePaymentResponse(
                decision.DecisionId,
                decision.PaymentId,
                decision.SelectedProviderId,
                selectedProvider.ProviderName,
                decision.AlternativeProviderIds,
                decision.Strategy,
                decision.DecisionReason,
                decision.GetCostEstimate(),
                decision.DecisionMadeAt);

            // Publish event
            var routeSelectedEvent = new PaymentRouteSelectedEvent(
                decision.DecisionId,
                decision.PaymentId,
                decision.SelectedProviderId,
                decision.AlternativeProviderIds,
                decision.Strategy,
                decision.GetCostEstimate(),
                decision.DecisionMadeAt);

            await eventPublisher.PublishAsync(routeSelectedEvent, cancellationToken);

            logger.LogInformation(
                "Routing decision {DecisionId} completed for payment {PaymentId}. Provider: {ProviderId}, Cost: {Cost}",
                decision.DecisionId,
                request.PaymentId,
                decision.SelectedProviderId,
                decision.GetCostEstimate().Amount);

            // Record routing metrics with readable labels
            stopwatch.Stop();
            MetricHelper.RecordRoutingDecision(
                strategy: decision.Strategy.ToString().ToLowerInvariant(),
                decisionReason: decision.DecisionReason.ToLowerInvariant(),
                eligibleProviderCount: eligibleProviders.Count,
                durationSeconds: stopwatch.Elapsed.TotalSeconds
            );

            return Result<RoutePaymentResponse>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error making routing decision for payment {PaymentId}", request.PaymentId);
            return Result<RoutePaymentResponse>.Failure(
                new Error("Routing.Error", "Error making routing decision"));
        }
    }
}
