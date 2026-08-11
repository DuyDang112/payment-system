using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RiskAssessment.Domain.Models;

namespace RiskAssessment.Infrastructure.VelocityChecking;

/// <summary>
/// In-memory velocity checker for development (can be replaced with Redis implementation)
/// </summary>
public sealed class VelocityChecker : IVelocityChecker
{
    private readonly ILogger<VelocityChecker> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    // Simple in-memory storage for development
    // In production, this should use Redis
    private static readonly Dictionary<string, VelocityCounter> _counters = new();

    public VelocityChecker(ILogger<VelocityChecker> logger)
    {
        _logger = logger;
    }

    public async Task<VelocityCheckResult> CheckVelocityAsync(
        string customerId,
        string merchantId,
        string windowType,
        int limit,
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var key = GetVelocityKey(customerId, merchantId, windowType);

            if (_counters.TryGetValue(key, out var counter))
            {
                // Check if counter has expired
                if (DateTime.UtcNow > counter.ExpiresAt)
                {
                    _counters.Remove(key);
                    return VelocityCheckResult.NotExceeded();
                }

                if (counter.Count >= limit)
                {
                    _logger.LogWarning(
                        "Velocity limit exceeded for customer {CustomerId} in window {WindowType}: {Count}/{Limit}",
                        customerId, windowType, counter.Count, limit);

                    return VelocityCheckResult.Exceeded(windowType, counter.Count, limit);
                }
            }

            return VelocityCheckResult.NotExceeded();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task IncrementCountAsync(
        string customerId,
        string merchantId,
        string windowType,
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var key = GetVelocityKey(customerId, merchantId, windowType);
            var windowSeconds = GetWindowSeconds(windowType);
            var expiresAt = DateTime.UtcNow.AddSeconds(windowSeconds);

            if (_counters.TryGetValue(key, out var counter))
            {
                // Check if counter has expired
                if (DateTime.UtcNow > counter.ExpiresAt)
                {
                    // Reset counter
                    _counters[key] = new VelocityCounter(1, expiresAt);
                }
                else
                {
                    // Increment counter
                    _counters[key] = counter with { Count = counter.Count + 1 };
                }
            }
            else
            {
                // Create new counter
                _counters[key] = new VelocityCounter(1, expiresAt);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static string GetVelocityKey(string customerId, string merchantId, string windowType)
        => $"velocity:{customerId}:{merchantId}:{windowType}";

    private static int GetWindowSeconds(string windowType) => windowType.ToUpperInvariant() switch
    {
        "ONE_MINUTE" => 60,
        "FIVE_MINUTES" => 300,
        "ONE_HOUR" => 3600,
        "ONE_DAY" => 86400,
        _ => 60
    };

    /// <summary>
    /// Clean up expired counters (should be called periodically)
    /// </summary>
    public void CleanupExpiredCounters()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _counters
            .Where(kvp => now > kvp.Value.ExpiresAt)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _counters.Remove(key);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired velocity counters", expiredKeys.Count);
        }
    }
}

/// <summary>
/// Internal velocity counter record
/// </summary>
internal sealed record VelocityCounter(int Count, DateTime ExpiresAt);
