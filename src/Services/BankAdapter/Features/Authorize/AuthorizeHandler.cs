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
using Shared;
using Shared.Observability;
using System.Diagnostics;

namespace BankAdapter.Features.Authorize;

/// <summary>
/// Handler for payment authorization requests
/// </summary>
internal sealed class AuthorizeHandler(
    ProviderFactory providerFactory,
    RequestLoggingService loggingService,
    RateLimiter rateLimiter,
    RetryPolicyService retryPolicy,
    IEventPublisher eventPublisher,
    BankAdapterDbContext context,
    ILogger<AuthorizeHandler> logger) : IAuthorizeHandler
{
    public async Task<Result<AuthorizeResponse>> HandleAsync(
        AuthorizeRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("Processing authorization request for payment {PaymentId}", request.PaymentId);

        // Validate amount
        if (request.Amount <= 0)
        {
            return Result<AuthorizeResponse>.Failure(ProviderErrors.InvalidRequest);
        }

        // Validate currency
        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length != 3)
        {
            return Result<AuthorizeResponse>.Failure(ProviderErrors.InvalidRequest);
        }

        // Get provider
        var providerResult = await providerFactory.GetProviderAsync(request.ProviderId, cancellationToken);
        if (providerResult.IsError)
        {
            return Result<AuthorizeResponse>.Failure(providerResult.Errors);
        }

        var provider = providerResult.Value;

        // Check if provider supports authorization
        if (!provider.SupportsOperation(Operation.Authorize))
        {
            logger.LogWarning("Provider {ProviderId} does not support authorization", request.ProviderId);
            return Result<AuthorizeResponse>.Failure(ProviderErrors.OperationNotSupported);
        }

        // Get provider configuration for rate limits and retry config
        var providerConfig = await context.Providers
            .FirstOrDefaultAsync(p => p.ProviderId == request.ProviderId, cancellationToken);

        if (providerConfig == null)
        {
            return Result<AuthorizeResponse>.Failure(ProviderErrors.ProviderNotFound);
        }

        // Check rate limit
        var rateLimitResult = await rateLimiter.CheckRateLimitAsync(
            request.ProviderId,
            providerConfig.RateLimits,
            cancellationToken);

        if (rateLimitResult.IsError)
        {
            // Publish rate limit exceeded event
            var (currentRequests, maxRequests) = rateLimiter.GetUsage(request.ProviderId);
            await eventPublisher.PublishAsync(new ProviderRateLimitExceededEvent(
                request.ProviderId,
                currentRequests,
                maxRequests,
                DateTime.UtcNow
            ), cancellationToken);

            return Result<AuthorizeResponse>.Failure(rateLimitResult.Errors);
        }

        try
        {
            // Create request log
            var providerRequest = new ProviderRequest(
                request.PaymentId,
                request.Amount,
                request.Currency,
                request.Metadata);

            var logEntry = await loggingService.CreateLogAsync(
                request.PaymentId,
                request.ProviderId,
                Operation.Authorize,
                providerRequest,
                cancellationToken);

            // Record provider latency for authorization attempts
            var operationStopwatch = Stopwatch.StartNew();

            // Execute provider request with retry logic
            var providerResponse = await retryPolicy.ExecuteWithRetryAsync(
                request.ProviderId,
                providerConfig.RetryConfig,
                () => provider.AuthorizeAsync(providerRequest, cancellationToken),
                cancellationToken);

            if (providerResponse.IsError)
            {
                operationStopwatch.Stop();

                await loggingService.UpdateLogErrorAsync(
                    logEntry,
                    new { error = providerResponse.Errors },
                    500,
                    providerResponse.Errors.FirstOrDefault()?.Code,
                    providerResponse.Errors.FirstOrDefault()?.Message,
                    ProviderResult.FatalError,
                    cancellationToken);

                // Record provider error metrics
                var errorType = providerResponse.Errors.FirstOrDefault()?.Code ?? "unknown_error";
                MetricHelper.RecordProviderError(provider.ProviderType.ToString().ToLowerInvariant(), errorType);
                MetricHelper.RecordProviderLatency(provider.ProviderType.ToString().ToLowerInvariant(), "authorize", operationStopwatch.Elapsed.TotalSeconds);

                await eventPublisher.PublishAsync(new PaymentAuthorizationFailedEvent(
                    request.PaymentId,
                    request.ProviderId,
                    providerResponse.Errors.FirstOrDefault()?.Code ?? "unknown",
                    providerResponse.Errors.FirstOrDefault()?.Message ?? "Authorization failed",
                    request.Amount,
                    request.Currency,
                    request.Metadata,
                    DateTime.UtcNow
                ), cancellationToken);

                return Result<AuthorizeResponse>.Failure(providerResponse.Errors);
            }

            var response = providerResponse.Value;

            if (!response.Success)
            {
                operationStopwatch.Stop();

                await loggingService.UpdateLogErrorAsync(
                    logEntry,
                    response,
                    400,
                    response.ErrorCode,
                    response.ErrorMessage,
                    ProviderResult.FatalError,
                    cancellationToken);

                // Record provider error metrics
                var errorType = response.ErrorCode ?? "authorization_failed";
                MetricHelper.RecordProviderError(provider.ProviderType.ToString().ToLowerInvariant(), errorType);
                MetricHelper.RecordProviderLatency(provider.ProviderType.ToString().ToLowerInvariant(), "authorize", operationStopwatch.Elapsed.TotalSeconds);
               // MetricHelper.RecordBankLatency(provider.ProviderType.ToString().ToLowerInvariant(), "authorize", operationStopwatch.Elapsed.TotalSeconds);

                await eventPublisher.PublishAsync(new PaymentAuthorizationFailedEvent(
                    request.PaymentId,
                    request.ProviderId,
                    response.ErrorCode ?? "unknown",
                    response.ErrorMessage ?? "Authorization failed",
                    request.Amount,
                    request.Currency,
                    request.Metadata,
                    DateTime.UtcNow
                ), cancellationToken);

                return Result<AuthorizeResponse>.Failure(ProviderErrors.ProviderError(
                    request.ProviderId,
                    response.ErrorCode ?? "authorization_failed",
                    response.ErrorMessage ?? "Authorization failed"));
            }

            await loggingService.UpdateLogSuccessAsync(
                logEntry,
                response,
                200,
                cancellationToken);

            // Record provider latency for successful authorization
            operationStopwatch.Stop();
            MetricHelper.RecordProviderLatency(provider.ProviderType.ToString().ToLowerInvariant(), "authorize", operationStopwatch.Elapsed.TotalSeconds);

            var authorizeResponse = new AuthorizeResponse(
                request.PaymentId,
                request.ProviderId,
                response.ProviderTransactionId,
                true,
                null,
                null,
                response.Metadata);

            await eventPublisher.PublishAsync(new PaymentAuthorizationSucceededEvent(
                request.PaymentId,
                request.ProviderId,
                response.ProviderTransactionId,
                request.Amount,
                request.Currency,
                request.Metadata,
                DateTime.UtcNow
            ), cancellationToken);

            logger.LogInformation(
                "Authorization succeeded for payment {PaymentId} with provider transaction ID {ProviderTransactionId}",
                request.PaymentId,
                response.ProviderTransactionId);

            return authorizeResponse;
        }
        finally
        {
            rateLimiter.ReleaseSlot(request.ProviderId);
        }
    }
}
