using System.Collections.Concurrent;
using BankAdapter.Domain;
using BankAdapter.Features.Shared.Errors;
using BankAdapter.Shared;

namespace BankAdapter.Infrastructure.RateLimiting;

/// <summary>
/// Rate limiting service for provider requests
/// </summary>
public sealed class RateLimiter(ILogger<RateLimiter> logger)
{
    private readonly ConcurrentDictionary<string, ProviderRateLimit> _rateLimits = new();

    /// <summary>
    /// Check if request is allowed under rate limit
    /// </summary>
    public async Task<Result<bool>> CheckRateLimitAsync(
        string providerId,
        RateLimits limits,
        CancellationToken cancellationToken = default)
    {
        var rateLimit = _rateLimits.GetOrAdd(providerId, _ => new ProviderRateLimit(limits));

        if (!await rateLimit.TryAcquireAsync(cancellationToken))
        {
            logger.LogWarning("Rate limit exceeded for provider {ProviderId}", providerId);
            return ProviderErrors.ProviderRateLimitExceeded;
        }

        return true;
    }

    /// <summary>
    /// Release rate limit slot after request completion
    /// </summary>
    public void ReleaseSlot(string providerId)
    {
        if (_rateLimits.TryGetValue(providerId, out var rateLimit))
        {
            rateLimit.Release();
        }
    }

    /// <summary>
    /// Reset rate limits for provider
    /// </summary>
    public void ResetRateLimit(string providerId)
    {
        _rateLimits.TryRemove(providerId, out _);
        logger.LogInformation("Reset rate limit for provider {ProviderId}", providerId);
    }

    /// <summary>
    /// Get current rate limit usage for provider
    /// </summary>
    public (int CurrentRequests, int MaxRequests) GetUsage(string providerId)
    {
        if (_rateLimits.TryGetValue(providerId, out var rateLimit))
        {
            return (rateLimit.CurrentRequests, rateLimit.MaxRequests);
        }

        return (0, 0);
    }
}

/// <summary>
/// Per-provider rate limit tracking
/// </summary>
internal sealed class ProviderRateLimit
{
    private readonly SemaphoreSlim _semaphore;
    private readonly RateLimits _limits;
    private readonly Queue<DateTime> _requestTimestamps;
    private readonly object _lock = new();
    private readonly Timer _cleanupTimer;

    public int MaxRequests => _limits.MaxConcurrentRequests;
    public int CurrentRequests => _semaphore.CurrentCount;

    public ProviderRateLimit(RateLimits limits)
    {
        _limits = limits;
        _semaphore = new SemaphoreSlim(limits.MaxConcurrentRequests, limits.MaxConcurrentRequests);
        _requestTimestamps = new Queue<DateTime>();
        _cleanupTimer = new Timer(CleanupOldTimestamps, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Try to acquire a rate limit slot
    /// </summary>
    public async Task<bool> TryAcquireAsync(CancellationToken cancellationToken)
    {
        // First check per-second rate limit
        if (!CheckPerSecondLimit())
        {
            return false;
        }

        // Then try to acquire concurrent slot
        return await _semaphore.WaitAsync(0, cancellationToken);
    }

    /// <summary>
    /// Release rate limit slot
    /// </summary>
    public void Release()
    {
        _semaphore.Release();
    }

    /// <summary>
    /// Check if per-second rate limit allows the request
    /// </summary>
    private bool CheckPerSecondLimit()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var oneSecondAgo = now.AddSeconds(-1);

            // Remove timestamps older than 1 second
            while (_requestTimestamps.Count > 0 && _requestTimestamps.Peek() < oneSecondAgo)
            {
                _requestTimestamps.Dequeue();
            }

            // Check if adding new request would exceed limit
            if (_requestTimestamps.Count >= _limits.MaxRequestsPerSecond)
            {
                return false;
            }

            _requestTimestamps.Enqueue(now);
            return true;
        }
    }

    /// <summary>
    /// Cleanup old timestamps periodically
    /// </summary>
    private void CleanupOldTimestamps(object? state)
    {
        lock (_lock)
        {
            var oneSecondAgo = DateTime.UtcNow.AddSeconds(-1);

            while (_requestTimestamps.Count > 0 && _requestTimestamps.Peek() < oneSecondAgo)
            {
                _requestTimestamps.Dequeue();
            }
        }
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _semaphore?.Dispose();
    }
}
