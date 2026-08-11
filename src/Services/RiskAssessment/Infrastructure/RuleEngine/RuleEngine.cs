using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RiskAssessment.Domain.Models;
using RiskAssessment.Features.EvaluateRisk;
using RiskAssessment.Infrastructure.Data;

namespace RiskAssessment.Infrastructure.RuleEngine;

/// <summary>
/// Core rule engine for evaluating risk rules
/// </summary>
public sealed class RuleEngine : IRuleEngine
{
    private readonly RiskAssessmentDbContext _context;
    private readonly IEnumerable<IRuleEvaluator> _evaluators;
    private readonly ILogger<RuleEngine> _logger;

    public RuleEngine(
        RiskAssessmentDbContext context,
        IEnumerable<IRuleEvaluator> evaluators,
        ILogger<RuleEngine> logger)
    {
        _context = context;
        _evaluators = evaluators;
        _logger = logger;
    }

    public async Task<RuleEngineResult> EvaluateAsync(
        EvaluateRiskRequest request,
        MerchantRiskProfile profile,
        CancellationToken cancellationToken)
    {
        var triggeredRules = new List<RuleTrigger>();
        int totalScore = 0;
        bool shouldBlock = false;

        try
        {
            // Load active rules by priority, ordered by priority
            var activeRules = await _context.RiskRules
                .Where(r => r.IsActive && r.DeletedAt == null)
                .OrderBy(r => r.Priority)
                .ToListAsync(cancellationToken);

            // Filter by merchant's enabled rules
            var enabledRules = activeRules
                .Where(r => profile.EnabledRuleIds.Contains(r.RuleId))
                .ToList();

            _logger.LogInformation(
                "Evaluating {RuleCount} active rules for merchant {MerchantId}",
                enabledRules.Count,
                request.MerchantId);

            // Evaluate each rule
            foreach (var rule in enabledRules)
            {
                var ruleTypeEnum = Enum.Parse<RuleType>(rule.RuleType);
                var evaluator = _evaluators.FirstOrDefault(e => e.CanEvaluate(ruleTypeEnum));

                if (evaluator == null)
                {
                    _logger.LogWarning("No evaluator found for rule type: {RuleType}", rule.RuleType);
                    continue;
                }

                var result = await evaluator.EvaluateAsync(rule, request, cancellationToken);

                if (result.IsTriggered)
                {
                    var actionEnum = Enum.Parse<RuleAction>(rule.Action);
                    var trigger = RuleTrigger.Create(
                        rule.RuleId,
                        rule.RuleName,
                        ruleTypeEnum,
                        actionEnum,
                        rule.ScoreImpact,
                        result.Message);

                    triggeredRules.Add(trigger);
                    totalScore += rule.ScoreImpact;

                    _logger.LogInformation(
                        "Rule {RuleId} triggered: {RuleName} - Score Impact: {ScoreImpact}",
                        rule.RuleId,
                        rule.RuleName,
                        rule.ScoreImpact);

                    // Early exit for BLOCK actions
                    if (actionEnum == RuleAction.BLOCK)
                    {
                        _logger.LogInformation(
                            "Rule {RuleId} triggered BLOCK action - stopping evaluation",
                            rule.RuleId);
                        shouldBlock = true;
                        break;
                    }
                }
            }

            // Clamp total score between 0-100
            totalScore = Math.Min(100, Math.Max(0, totalScore));

            return new RuleEngineResult(triggeredRules, totalScore, shouldBlock);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during rule evaluation for payment {PaymentId}", request.PaymentId);
            return new RuleEngineResult(new List<RuleTrigger>(), 0, false);
        }
    }
}
