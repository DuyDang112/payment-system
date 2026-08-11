using BankAdapter.Domain;
using BankAdapter.Features.Shared.Errors;
using BankAdapter.Features.Shared.Routes;
using BankAdapter.Infrastructure.Data;
using BankAdapter.Infrastructure.Events;
using BankAdapter.Infrastructure.Logging;
using BankAdapter.Infrastructure.Providers;
using BankAdapter.Infrastructure.RateLimiting;
using BankAdapter.Infrastructure.Retry;
using BankAdapter.Shared;
using Microsoft.EntityFrameworkCore;

namespace BankAdapter.Features.Refund;

/// <summary>
/// Handler for payment refund requests
/// </summary>
internal sealed class RefundHandler(
    ProviderFactory providerFactory,
    RequestLoggingService loggingService,
    RateLimiter rateLimiter,
    RetryPolicyService retryPolicy,
    IEventPublisher eventPublisher,
    BankAdapterDbContext context,
    ILogger<RefundHandler> logger) : IRefundHandler
{
    public async Task<Result<RefundResponse>> HandleAsync(
        RefundRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing refund request for payment {PaymentId}", request.PaymentId);

        // Validate amount
        if (request.Amount <= 0)
        {
            return Result<RefundResponse>.Failure(ProviderErrors.InvalidRequest);
        }

        // Validate authorization token
        if (string.IsNullOrWhiteSpace(request.AuthorizationToken))
        {
            return Result<RefundResponse>.Failure(ProviderErrors.MissingAuthorizationToken);
        }

        // Get provider
        var providerResult = await providerFactory.GetProviderAsync(request.ProviderId, cancellationToken);
        if (providerResult.IsError)
        {
            return Result<RefundResponse>.Failure(providerResult.Errors);
        }

        var provider = providerResult.Value;

        // Check if provider supports refund
        if (!provider.SupportsOperation(Operation.Refund))
        {
            logger.LogWarning("Provider {ProviderId} does not support refund", request.ProviderId);
            return Result<RefundResponse>.Failure(ProviderErrors.OperationNotSupported);
        }

        // Get provider configuration
        var providerConfig = await context.Providers
            .FirstOrDefaultAsync(p => p.ProviderId == request.ProviderId, cancellationToken);

        if (providerConfig == null)
        {
            return Result<RefundResponse>.Failure(ProviderErrors.ProviderNotFound);
        }

        // Check rate limit
        var rateLimitResult = await rateLimiter.CheckRateLimitAsync(
            request.ProviderId,
            providerConfig.RateLimits,
            cancellationToken);

        if (rateLimitResult.IsError)
        {
            var (currentRequests, maxRequests) = rateLimiter.GetUsage(request.ProviderId);
            await eventPublisher.PublishAsync(new ProviderRateLimitExceededEvent(
                request.ProviderId,
                currentRequests,
                maxRequests,
                DateTime.UtcNow
            ), cancellationToken);

            return Result<RefundResponse>.Failure(rateLimitResult.Errors);
        }

        try
        {
            // Create request log
            var providerRequest = new ProviderRequest(
                request.PaymentId,
                request.Amount,
                request.Currency,
                request.Metadata,
                request.AuthorizationToken);

            var logEntry = await loggingService.CreateLogAsync(
                request.PaymentId,
                request.ProviderId,
                Operation.Refund,
                providerRequest,
                cancellationToken);

            // Execute provider request with retry logic
            var providerResponse = await retryPolicy.ExecuteWithRetryAsync(
                request.ProviderId,
                providerConfig.RetryConfig,
                () => provider.RefundAsync(providerRequest, cancellationToken),
                cancellationToken);

            if (providerResponse.IsError)
            {
                await loggingService.UpdateLogErrorAsync(
                    logEntry,
                    new { error = providerResponse.Errors },
                    500,
                    providerResponse.Errors.FirstOrDefault()?.Code,
                    providerResponse.Errors.FirstOrDefault()?.Message,
                    ProviderResult.FatalError,
                    cancellationToken);

                await eventPublisher.PublishAsync(new PaymentRefundFailedEvent(
                    request.PaymentId,
                    request.ProviderId,
                    providerResponse.Errors.FirstOrDefault()?.Code ?? "unknown",
                    providerResponse.Errors.FirstOrDefault()?.Message ?? "Refund failed",
                    request.Amount,
                    request.Currency,
                    request.Metadata,
                    DateTime.UtcNow
                ), cancellationToken);

                return Result<RefundResponse>.Failure(providerResponse.Errors);
            }

            var response = providerResponse.Value;

            if (!response.Success)
            {
                await loggingService.UpdateLogErrorAsync(
                    logEntry,
                    response,
                    400,
                    response.ErrorCode,
                    response.ErrorMessage,
                    ProviderResult.FatalError,
                    cancellationToken);

                await eventPublisher.PublishAsync(new PaymentRefundFailedEvent(
                    request.PaymentId,
                    request.ProviderId,
                    response.ErrorCode ?? "unknown",
                    response.ErrorMessage ?? "Refund failed",
                    request.Amount,
                    request.Currency,
                    request.Metadata,
                    DateTime.UtcNow
                ), cancellationToken);

                return Result<RefundResponse>.Failure(ProviderErrors.ProviderError(
                    request.ProviderId,
                    response.ErrorCode ?? "refund_failed",
                    response.ErrorMessage ?? "Refund failed"));
            }

            await loggingService.UpdateLogSuccessAsync(
                logEntry,
                response,
                200,
                cancellationToken);

            var refundResponse = new RefundResponse(
                request.PaymentId,
                request.ProviderId,
                response.ProviderTransactionId,
                true,
                null,
                null,
                response.Metadata);

            await eventPublisher.PublishAsync(new PaymentRefundSucceededEvent(
                request.PaymentId,
                request.ProviderId,
                response.ProviderTransactionId,
                request.Amount,
                request.Currency,
                request.Metadata,
                DateTime.UtcNow
            ), cancellationToken);

            logger.LogInformation(
                "Refund succeeded for payment {PaymentId} with provider transaction ID {ProviderTransactionId}",
                request.PaymentId,
                response.ProviderTransactionId);

            return refundResponse;
        }
        finally
        {
            rateLimiter.ReleaseSlot(request.ProviderId);
        }
    }
}
