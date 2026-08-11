using RiskAssessment.Domain.Models;

namespace RiskAssessment.Features.EvaluateRisk;

/// <summary>
/// Mapping extensions for EvaluateRisk feature
/// </summary>
internal static class EvaluateRiskMappingExtensions
{
    /// <summary>
    /// Map RiskEvaluation to EvaluateRiskResponse
    /// </summary>
    public static EvaluateRiskResponse MapToResponse(this RiskEvaluation evaluation)
    {
        return new EvaluateRiskResponse
        {
            EvaluationId = evaluation.EvaluationId,
            PaymentId = evaluation.PaymentId,
            RiskScore = evaluation.RiskScore,
            Decision = evaluation.Decision,
            EvaluatedAt = evaluation.EvaluatedAt,
            RuleVersion = evaluation.RuleVersion,
            TriggeredRules = evaluation.TriggeredRules.Select(t => new TriggeredRuleDto
            {
                RuleId = t.RuleId,
                RuleName = t.RuleName,
                RuleType = t.RuleType.ToString(),
                Action = t.Action.ToString(),
                ScoreImpact = t.ScoreImpact,
                Description = t.Description
            }).ToList(),
            TimeoutOccurred = false
        };
    }
}
