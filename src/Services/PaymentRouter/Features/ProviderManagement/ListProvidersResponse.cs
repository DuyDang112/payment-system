using PaymentRouter.Domain.Models;

namespace PaymentRouter.Features.ProviderManagement;

/// <summary>
/// Response containing list of payment providers
/// </summary>
public sealed record ListProvidersResponse(
    ProviderDto[] Providers
);

public sealed record ProviderDto(
    string ProviderId,
    string ProviderName,
    ProviderType ProviderType,
    string[] SupportedCurrencies,
    PaymentMethod[] SupportedMethods,
    int Priority,
    bool IsEnabled,
    HealthStatus HealthStatus,
    CircuitState CircuitBreakerState,
    PerformanceMetricsDto PerformanceMetrics
);

public sealed record PerformanceMetricsDto(
    double SuccessRate,
    double P50Latency,
    double P95Latency,
    double P99Latency,
    int DailyVolume,
    double FailureRate,
    int ConsecutiveFailures
);
