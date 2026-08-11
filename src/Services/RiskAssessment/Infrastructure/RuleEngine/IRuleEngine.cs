using RiskAssessment.Domain.Models;
using RiskAssessment.Features.EvaluateRisk;

namespace RiskAssessment.Infrastructure.RuleEngine;

/// <summary>
/// Interface for rule engine
/// </summary>
public interface IRuleEngine
{
    /// <summary>
    /// Evaluate risk rules against the request
    /// </summary>
    Task<RuleEngineResult> EvaluateAsync(
        EvaluateRiskRequest request,
        MerchantRiskProfile profile,
        CancellationToken cancellationToken);
}

/// <summary>
/// Result of rule engine evaluation
/// </summary>
public sealed record RuleEngineResult(
    List<RuleTrigger> TriggeredRules,
    int TotalScore,
    bool ShouldBlock);
