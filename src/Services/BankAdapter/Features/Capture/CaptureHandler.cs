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

namespace BankAdapter.Features.Capture;

/// <summary>
/// Handler for payment capture requests
/// </summary>
internal sealed class CaptureHandler(
    ProviderFactory providerFactory,
    RequestLoggingService loggingService,
    RateLimiter rateLimiter,
    RetryPolicyService retryPolicy,
    IEventPublisher eventPublisher,
    BankAdapterDbContext context,
    ILogger<CaptureHandler> logger) : ICaptureHandler
{
    public async Task<Result<CaptureResponse>> HandleAsync(
        CaptureRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing capture request for payment {PaymentId}", request.PaymentId);

        // Validate amount
        if (request.Amount <= 0)
        {
            return Result<CaptureResponse>.Failure(ProviderErrors.InvalidRequest);
        }

        // Validate authorization token
        if (string.IsNullOrWhiteSpace(request.AuthorizationToken))
        {
            return Result<CaptureResponse>.Failure(ProviderErrors.MissingAuthorizationToken);
        }

        // Get provider
        var providerResult = await providerFactory.GetProviderAsync(request.ProviderId, cancellationToken);
        if (providerResult.IsError)
        {
            return Result<CaptureResponse>.Failure(providerResult.Errors);
        }

        var provider = providerResult.Value;

        // Check if provider supports capture
        if (!provider.SupportsOperation(Operation.Capture))
        {
            logger.LogWarning("Provider {ProviderId} does not support capture", request.ProviderId);
            return Result<CaptureResponse>.Failure(ProviderErrors.OperationNotSupported);
        }

        // Get provider configuration
        var providerConfig = await context.Providers
            .FirstOrDefaultAsync(p => p.ProviderId == request.ProviderId, cancellationToken);

        if (providerConfig == null)
        {
            return Result<CaptureResponse>.Failure(ProviderErrors.ProviderNotFound);
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

            return Result<CaptureResponse>.Failure(rateLimitResult.Errors);
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
                Operation.Capture,
                providerRequest,
                cancellationToken);

            // Execute provider request with retry logic
            var providerResponse = await retryPolicy.ExecuteWithRetryAsync(
                request.ProviderId,
                providerConfig.RetryConfig,
                () => provider.CaptureAsync(providerRequest, cancellationToken),
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

                await eventPublisher.PublishAsync(new PaymentCaptureFailedEvent(
                    request.PaymentId,
                    request.ProviderId,
                    providerResponse.Errors.FirstOrDefault()?.Code ?? "unknown",
                    providerResponse.Errors.FirstOrDefault()?.Message ?? "Capture failed",
                    request.Amount,
                    request.Currency,
                    request.Metadata,
                    DateTime.UtcNow
                ), cancellationToken);

                return Result<CaptureResponse>.Failure(providerResponse.Errors);
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

                await eventPublisher.PublishAsync(new PaymentCaptureFailedEvent(
                    request.PaymentId,
                    request.ProviderId,
                    response.ErrorCode ?? "unknown",
                    response.ErrorMessage ?? "Capture failed",
                    request.Amount,
                    request.Currency,
                    request.Metadata,
                    DateTime.UtcNow
                ), cancellationToken);

                return Result<CaptureResponse>.Failure(ProviderErrors.ProviderError(
                    request.ProviderId,
                    response.ErrorCode ?? "capture_failed",
                    response.ErrorMessage ?? "Capture failed"));
            }

            await loggingService.UpdateLogSuccessAsync(
                logEntry,
                response,
                200,
                cancellationToken);

            var captureResponse = new CaptureResponse(
                request.PaymentId,
                request.ProviderId,
                response.ProviderTransactionId,
                true,
                null,
                null,
                response.Metadata);

            await eventPublisher.PublishAsync(new PaymentCaptureSucceededEvent(
                request.PaymentId,
                request.ProviderId,
                response.ProviderTransactionId,
                request.Amount,
                request.Currency,
                request.Metadata,
                DateTime.UtcNow
            ), cancellationToken);

            logger.LogInformation(
                "Capture succeeded for payment {PaymentId} with provider transaction ID {ProviderTransactionId}",
                request.PaymentId,
                response.ProviderTransactionId);

            return captureResponse;
        }
        finally
        {
            rateLimiter.ReleaseSlot(request.ProviderId);
        }
    }
}
