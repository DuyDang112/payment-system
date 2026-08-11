using RiskAssessment.Domain.Models;
using RiskAssessment.Features.EvaluateRisk;

namespace RiskAssessment.Infrastructure.RuleEngine;

/// <summary>
/// Interface for rule evaluators
/// </summary>
public interface IRuleEvaluator
{
    /// <summary>
    /// Check if this evaluator can handle the specified rule type
    /// </summary>
    bool CanEvaluate(RuleType ruleType);

    /// <summary>
    /// Evaluate the rule against the request
    /// </summary>
    Task<RuleEvaluationResult> EvaluateAsync(
        RiskRule rule,
        EvaluateRiskRequest request,
        CancellationToken cancellationToken);
}
