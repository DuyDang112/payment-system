using RiskAssessment.Domain.Models;
using RiskAssessment.Features.EvaluateRisk;
using System.Text.Json;

namespace RiskAssessment.Infrastructure.RuleEngine;

/// <summary>
/// Velocity rule evaluator
/// </summary>
public sealed class VelocityRuleEvaluator : IRuleEvaluator
{
    public bool CanEvaluate(RuleType ruleType) => ruleType == RuleType.VELOCITY;

    public Task<RuleEvaluationResult> EvaluateAsync(
        RiskRule rule,
        EvaluateRiskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var conditions = JsonSerializer.Deserialize<VelocityRuleConditions>(rule.ConditionsJson);
            if (conditions == null)
            {
                return Task.FromResult(RuleEvaluationResult.NotTriggered());
            }

            // For now, return not triggered - actual velocity checking will be done in the handler
            // This evaluator just validates the rule can be evaluated
            return Task.FromResult(RuleEvaluationResult.NotTriggered());
        }
        catch (JsonException)
        {
            return Task.FromResult(RuleEvaluationResult.NotTriggered());
        }
    }
}
