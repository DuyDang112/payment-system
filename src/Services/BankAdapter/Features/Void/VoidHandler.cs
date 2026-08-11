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

namespace BankAdapter.Features.Void;

/// <summary>
/// Handler for payment void requests
/// </summary>
internal sealed class VoidHandler(
    ProviderFactory providerFactory,
    RequestLoggingService loggingService,
    RateLimiter rateLimiter,
    RetryPolicyService retryPolicy,
    IEventPublisher eventPublisher,
    BankAdapterDbContext context,
    ILogger<VoidHandler> logger) : IVoidHandler
{
    public async Task<Result<VoidResponse>> HandleAsync(
        VoidRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing void request for payment {PaymentId}", request.PaymentId);

        // Validate amount
        if (request.Amount <= 0)
        {
            return Result<VoidResponse>.Failure(ProviderErrors.InvalidRequest);
        }

        // Validate authorization token
        if (string.IsNullOrWhiteSpace(request.AuthorizationToken))
        {
            return Result<VoidResponse>.Failure(ProviderErrors.MissingAuthorizationToken);
        }

        // Get provider
        var providerResult = await providerFactory.GetProviderAsync(request.ProviderId, cancellationToken);
        if (providerResult.IsError)
        {
            return Result<VoidResponse>.Failure(providerResult.Errors);
        }

        var provider = providerResult.Value;

        // Check if provider supports void
        if (!provider.SupportsOperation(Operation.Void))
        {
            logger.LogWarning("Provider {ProviderId} does not support void", request.ProviderId);
            return Result<VoidResponse>.Failure(ProviderErrors.OperationNotSupported);
        }

        // Get provider configuration
        var providerConfig = await context.Providers
            .FirstOrDefaultAsync(p => p.ProviderId == request.ProviderId, cancellationToken);

        if (providerConfig == null)
        {
            return Result<VoidResponse>.Failure(ProviderErrors.ProviderNotFound);
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

            return Result<VoidResponse>.Failure(rateLimitResult.Errors);
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
                Operation.Void,
                providerRequest,
                cancellationToken);

            // Execute provider request with retry logic
            var providerResponse = await retryPolicy.ExecuteWithRetryAsync(
                request.ProviderId,
                providerConfig.RetryConfig,
                () => provider.VoidAsync(providerRequest, cancellationToken),
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

                return Result<VoidResponse>.Failure(providerResponse.Errors);
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

                return Result<VoidResponse>.Failure(ProviderErrors.ProviderError(
                    request.ProviderId,
                    response.ErrorCode ?? "void_failed",
                    response.ErrorMessage ?? "Void failed"));
            }

            await loggingService.UpdateLogSuccessAsync(
                logEntry,
                response,
                200,
                cancellationToken);

            var voidResponse = new VoidResponse(
                request.PaymentId,
                request.ProviderId,
                response.ProviderTransactionId,
                true,
                null,
                null,
                response.Metadata);

            logger.LogInformation(
                "Void succeeded for payment {PaymentId} with provider transaction ID {ProviderTransactionId}",
                request.PaymentId,
                response.ProviderTransactionId);

            return voidResponse;
        }
        finally
        {
            rateLimiter.ReleaseSlot(request.ProviderId);
        }
    }
}
