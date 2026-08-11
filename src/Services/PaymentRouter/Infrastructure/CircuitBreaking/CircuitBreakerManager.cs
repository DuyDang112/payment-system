using PaymentRouter.Domain.Models;

namespace PaymentRouter.Infrastructure.CircuitBreaking;

/// <summary>
/// Manages circuit breaker state for payment providers
/// </summary>
public interface ICircuitBreakerManager
{
    bool IsProviderAvailable(string providerId);
    void RecordSuccess(string providerId);
    void RecordFailure(string providerId);
    Task<TestResult> TestProviderAsync(string providerId, CancellationToken cancellationToken);
}

public sealed record TestResult(bool IsHealthy, string? Message = null);

public sealed class CircuitBreakerManager : ICircuitBreakerManager
{
    private const int FAILURE_THRESHOLD = 5;
    private readonly Dictionary<string, CircuitBreakerState> _states = new();
    private readonly ILogger<CircuitBreakerManager> _logger;

    public CircuitBreakerManager(ILogger<CircuitBreakerManager> logger)
    {
        _logger = logger;
    }

    public bool IsProviderAvailable(string providerId)
    {
        if (!_states.TryGetValue(providerId, out var state))
        {
            _states[providerId] = new CircuitBreakerState();
            return true;
        }

        return state.State != CircuitState.OPEN;
    }

    public void RecordSuccess(string providerId)
    {
        if (!_states.TryGetValue(providerId, out var state))
        {
            _states[providerId] = new CircuitBreakerState();
            return;
        }

        state.ConsecutiveFailures = 0;
        state.State = CircuitState.CLOSED;
        state.LastStateChange = DateTime.UtcNow;

        _logger.LogInformation("Circuit breaker CLOSED for provider {ProviderId}", providerId);
    }

    public void RecordFailure(string providerId)
    {
        if (!_states.TryGetValue(providerId, out var state))
        {
            _states[providerId] = new CircuitBreakerState();
        }

        state.ConsecutiveFailures++;
        state.LastFailureTime = DateTime.UtcNow;

        if (state.ConsecutiveFailures >= FAILURE_THRESHOLD && state.State != CircuitState.OPEN)
        {
            state.State = CircuitState.OPEN;
            state.LastStateChange = DateTime.UtcNow;

            _logger.LogWarning(
                "Circuit breaker OPENED for provider {ProviderId} after {Failures} consecutive failures",
                providerId,
                state.ConsecutiveFailures);
        }
        else
        {
            _logger.LogWarning(
                "Failure recorded for provider {ProviderId}. Consecutive failures: {Failures}",
                providerId,
                state.ConsecutiveFailures);
        }
    }

    public async Task<TestResult> TestProviderAsync(string providerId, CancellationToken cancellationToken)
    {
        if (!_states.TryGetValue(providerId, out var state))
        {
            return new TestResult(true, "Provider not tracked");
        }

        if (state.State != CircuitState.OPEN)
        {
            return new TestResult(true, "Circuit is not open");
        }

        // Minimum time before attempting recovery (1 minute)
        var timeSinceLastFailure = DateTime.UtcNow - state.LastFailureTime;
        if (timeSinceLastFailure < TimeSpan.FromMinutes(1))
        {
            return new TestResult(false, "Too soon to test");
        }

        // Move to HALF_OPEN state
        state.State = CircuitState.HALF_OPEN;
        state.LastStateChange = DateTime.UtcNow;

        _logger.LogInformation("Circuit breaker moved to HALF_OPEN for provider {ProviderId}", providerId);

        // In a real implementation, this would perform an actual health check
        // For now, we'll simulate a successful test
        await Task.Delay(10, cancellationToken);

        return new TestResult(true, "Circuit moved to HALF_OPEN");
    }

    private sealed class CircuitBreakerState
    {
        public CircuitState State { get; set; } = CircuitState.CLOSED;
        public int ConsecutiveFailures { get; set; }
        public DateTime LastFailureTime { get; set; }
        public DateTime LastStateChange { get; set; } = DateTime.UtcNow;
    }
}
