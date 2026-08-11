using RiskAssessment.Domain.Models;
using RiskAssessment.Features.EvaluateRisk;
using System.Text.Json;

namespace RiskAssessment.Infrastructure.RuleEngine;

/// <summary>
/// Amount rule evaluator
/// </summary>
public sealed class AmountRuleEvaluator : IRuleEvaluator
{
    public bool CanEvaluate(RuleType ruleType) => ruleType == RuleType.AMOUNT;

    public Task<RuleEvaluationResult> EvaluateAsync(
        RiskRule rule,
        EvaluateRiskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var conditions = JsonSerializer.Deserialize<AmountRuleConditions>(rule.ConditionsJson);
            if (conditions == null || !request.Amount.HasValue || !conditions.MinAmount.HasValue)
            {
                return Task.FromResult(RuleEvaluationResult.NotTriggered());
            }

            var isTriggered = conditions.Operator.ToUpperInvariant() switch
            {
                "GREATER_THAN" => request.Amount.Value > conditions.MinAmount.Value,
                "LESS_THAN" => request.Amount.Value < conditions.MinAmount.Value,
                "EQUALS" => request.Amount.Value == conditions.MinAmount.Value,
                "BETWEEN" => conditions.MaxAmount.HasValue &&
                             request.Amount.Value >= conditions.MinAmount.Value &&
                             request.Amount.Value <= conditions.MaxAmount.Value,
                _ => false
            };

            return isTriggered
                ? Task.FromResult(RuleEvaluationResult.Triggered(
                    $"Amount check triggered: {request.Amount.Value}"))
                : Task.FromResult(RuleEvaluationResult.NotTriggered());
        }
        catch (JsonException)
        {
            return Task.FromResult(RuleEvaluationResult.NotTriggered());
        }
    }
}
