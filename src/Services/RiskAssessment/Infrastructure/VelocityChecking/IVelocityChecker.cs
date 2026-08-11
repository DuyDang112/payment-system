using RiskAssessment.Domain.Models;

namespace RiskAssessment.Infrastructure.VelocityChecking;

/// <summary>
/// Interface for velocity checking
/// </summary>
public interface IVelocityChecker
{
    /// <summary>
    /// Check if velocity limit is exceeded
    /// </summary>
    Task<VelocityCheckResult> CheckVelocityAsync(
        string customerId,
        string merchantId,
        string windowType,
        int limit,
        CancellationToken cancellationToken);

    /// <summary>
    /// Increment velocity counter
    /// </summary>
    Task IncrementCountAsync(
        string customerId,
        string merchantId,
        string windowType,
        CancellationToken cancellationToken);
}
