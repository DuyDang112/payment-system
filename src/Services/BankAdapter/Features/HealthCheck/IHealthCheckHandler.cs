using BankAdapter.Shared;

namespace BankAdapter.Features.HealthCheck;

/// <summary>
/// Interface for health check handler
/// </summary>
public interface IHealthCheckHandler : IHandler
{
    Task<Result<HealthCheckResponse>> HandleAsync(string providerId, CancellationToken cancellationToken);
}
