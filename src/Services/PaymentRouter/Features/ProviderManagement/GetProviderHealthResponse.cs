using PaymentRouter.Domain.Models;

namespace PaymentRouter.Features.ProviderManagement;

/// <summary>
/// Response containing provider health information
/// </summary>
public sealed record GetProviderHealthResponse(
    string ProviderId,
    string ProviderName,
    HealthStatus HealthStatus,
    CircuitState CircuitBreakerState,
    DateTime LastHealthCheck,
    PerformanceHealthDto PerformanceHealth
);

public sealed record PerformanceHealthDto(
    double SuccessRate,
    double FailureRate,
    int ConsecutiveFailures,
    double P50Latency,
    double P95Latency,
    double P99Latency
);
