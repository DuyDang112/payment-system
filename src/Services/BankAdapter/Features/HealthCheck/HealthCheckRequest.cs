namespace BankAdapter.Features.HealthCheck;

/// <summary>
/// Response for provider health check
/// </summary>
public sealed record HealthCheckResponse(
    string ProviderId,
    bool IsHealthy,
    string? Message = null,
    Dictionary<string, string>? Details = null
);
