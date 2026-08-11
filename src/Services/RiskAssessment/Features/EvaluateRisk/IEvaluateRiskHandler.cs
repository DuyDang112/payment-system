using RiskAssessment.Shared;

namespace RiskAssessment.Features.EvaluateRisk;

/// <summary>
/// Interface for EvaluateRisk handler
/// </summary>
public interface IEvaluateRiskHandler : IHandler
{
    /// <summary>
    /// Handle risk evaluation request
    /// </summary>
    Task<Result<EvaluateRiskResponse>> HandleAsync(
        EvaluateRiskRequest request,
        CancellationToken cancellationToken);
}
