namespace RiskAssessment.Domain.Models;

/// <summary>
/// Represents a triggered rule during evaluation
/// </summary>
public sealed record RuleTrigger(
    string RuleId,
    string RuleName,
    RuleType RuleType,
    RuleAction Action,
    int ScoreImpact,
    string? Description)
{
    public static RuleTrigger Create(
        string ruleId,
        string ruleName,
        RuleType ruleType,
        RuleAction action,
        int scoreImpact,
        string? description = null)
    {
        return new RuleTrigger(ruleId, ruleName, ruleType, action, scoreImpact, description);
    }
}
