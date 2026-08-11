using BankAdapter.Domain;
using BankAdapter.Features.Shared.Errors;
using BankAdapter.Shared;
using Polly;
using Polly.Retry;

namespace BankAdapter.Infrastructure.Retry;

/// <summary>
/// Retry policy service for provider requests
/// </summary>
public sealed class RetryPolicyService(ILogger<RetryPolicyService> logger)
{
    /// <summary>
    /// Execute async operation with retry logic
    /// </summary>
    public async Task<Result<T>> ExecuteWithRetryAsync<T>(
        string providerId,
        RetryConfig config,
        Func<Task<Result<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        var retryCount = 0;
        var lastError = Error.None;

        while (retryCount <= config.MaxRetries)
        {
            try
            {
                if (retryCount > 0)
                {
                    logger.LogInformation(
                        "Retry attempt {RetryCount}/{MaxRetries} for provider {ProviderId}",
                        retryCount,
                        config.MaxRetries + 1,
                        providerId);

                    var backoff = CalculateBackoff(retryCount, config.BackoffMs);
                    await Task.Delay(backoff, cancellationToken);
                }

                var result = await operation();

                if (result.IsSuccess)
                {
                    if (retryCount > 0)
                    {
                        logger.LogInformation(
                            "Operation succeeded after {RetryCount} retries for provider {ProviderId}",
                            retryCount,
                            providerId);
                    }

                    return result;
                }

                // Check if error is retryable
                if (!IsRetryableError(result.Errors, config))
                {
                    logger.LogWarning(
                        "Non-retryable error encountered for provider {ProviderId}: {ErrorCode}",
                        providerId,
                        result.Errors.FirstOrDefault()?.Code);

                    return result;
                }

                lastError = result.Errors.FirstOrDefault() ?? Error.None;
                retryCount++;

                logger.LogInformation(
                    "Retryable error encountered for provider {ProviderId}: {ErrorCode}. Attempting retry...",
                    providerId,
                    lastError.Code);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Exception occurred during operation for provider {ProviderId}",
                    providerId);

                retryCount++;

                if (retryCount > config.MaxRetries)
                {
                    return ProviderErrors.ProviderError(
                        providerId,
                        "exception",
                        $"Operation failed after {retryCount} attempts: {ex.Message}");
                }
            }
        }

        logger.LogError(
            "Operation failed after {MaxRetries} retries for provider {ProviderId}",
            config.MaxRetries,
            providerId);

        return lastError == Error.None
            ? ProviderErrors.ProviderError(providerId, "max_retries_exceeded", "Maximum retry attempts exceeded")
            : lastError;
    }

    /// <summary>
    /// Calculate exponential backoff delay
    /// </summary>
    private static int CalculateBackoff(int retryCount, int baseBackoffMs)
    {
        // Exponential backoff with jitter
        var exponentialBackoff = baseBackoffMs * (int)Math.Pow(2, retryCount - 1);
        var jitter = Random.Shared.Next(0, baseBackoffMs / 4); // Add up to 25% jitter
        return exponentialBackoff + jitter;
    }

    /// <summary>
    /// Check if error is retryable based on configuration
    /// </summary>
    private bool IsRetryableError(List<Error> errors, RetryConfig config)
    {
        if (errors.Count == 0)
        {
            return false;
        }

        var error = errors[0];

        // Check if error code matches retryable errors
        foreach (var retryableError in config.RetryableErrors)
        {
            if (error.Code.Contains(retryableError, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Check for common retryable error patterns
        if (error.Code.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("rate_limit", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("transient", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// Polly-based async retry policy for HTTP requests
/// </summary>
public static class HttpRetryPolicy
{
    /// <summary>
    /// Get retry policy for HTTP requests
    /// </summary>
    public static AsyncRetryPolicy<HttpResponseMessage> GetPolicy(
        RetryConfig config,
        ILogger logger,
        string providerId)
    {
        return Policy
            .HandleResult<HttpResponseMessage>(r =>
                !r.IsSuccessStatusCode &&
                IsRetryableStatusCode(r.StatusCode))
            .Or<HttpRequestException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                config.MaxRetries,
                retryAttempt => CalculateBackoff(retryAttempt, config.BackoffMs),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    logger.LogWarning(
                        "Retry {RetryAttempt}/{MaxRetries} after {Delay}ms for provider {ProviderId} due to: {Reason}",
                        retryAttempt,
                        config.MaxRetries,
                        timespan.TotalMilliseconds,
                        providerId,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }

    /// <summary>
    /// Calculate exponential backoff delay
    /// </summary>
    private static TimeSpan CalculateBackoff(int retryAttempt, int baseBackoffMs)
    {
        var exponentialBackoff = baseBackoffMs * Math.Pow(2, retryAttempt - 1);
        var jitter = Random.Shared.Next(0, baseBackoffMs / 4);
        return TimeSpan.FromMilliseconds(exponentialBackoff + jitter);
    }

    /// <summary>
    /// Check if HTTP status code is retryable
    /// </summary>
    private static bool IsRetryableStatusCode(System.Net.HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            System.Net.HttpStatusCode.RequestTimeout => true, // 408
            System.Net.HttpStatusCode.TooManyRequests => true, // 429
            System.Net.HttpStatusCode.InternalServerError => true, // 500
            System.Net.HttpStatusCode.BadGateway => true, // 502
            System.Net.HttpStatusCode.ServiceUnavailable => true, // 503
            System.Net.HttpStatusCode.GatewayTimeout => true, // 504
            _ => false
        };
    }
}
