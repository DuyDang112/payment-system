using RiskAssessment.Domain.Models;
using RiskAssessment.Features.EvaluateRisk;
using System.Text.Json;

namespace RiskAssessment.Infrastructure.RuleEngine;

/// <summary>
/// Blacklist rule evaluator
/// </summary>
public sealed class BlacklistRuleEvaluator : IRuleEvaluator
{
    public bool CanEvaluate(RuleType ruleType) => ruleType == RuleType.BLACKLIST;

    public Task<RuleEvaluationResult> EvaluateAsync(
        RiskRule rule,
        EvaluateRiskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var conditions = JsonSerializer.Deserialize<BlacklistRuleConditions>(rule.ConditionsJson);
            if (conditions == null)
            {
                return Task.FromResult(RuleEvaluationResult.NotTriggered());
            }

            // Check if the entity is blacklisted
            var entityValue = GetEntityValue(request, conditions.EntityType);
            var isBlacklisted = conditions.EntityValue == "*" ||
                                (!string.IsNullOrEmpty(conditions.EntityValue) &&
                                 conditions.EntityValue == entityValue);

            if (isBlacklisted)
            {
                return Task.FromResult(RuleEvaluationResult.Triggered(
                    $"Entity blacklisted: {conditions.EntityType}"));
            }

            return Task.FromResult(RuleEvaluationResult.NotTriggered());
        }
        catch (JsonException)
        {
            return Task.FromResult(RuleEvaluationResult.NotTriggered());
        }
    }

    private static string? GetEntityValue(EvaluateRiskRequest request, string entityType)
    {
        return entityType switch
        {
            BlacklistEntry.EntityTypeCustomer => request.CustomerId,
            BlacklistEntry.EntityTypeIp => request.IpAddress,
            BlacklistEntry.EntityTypeEmail => request.CustomerEmail,
            BlacklistEntry.EntityTypeCard => request.PaymentMethodToken,
            _ => null
        };
    }
}
