using RiskAssessment.Domain.Models;
using RiskAssessment.Features.EvaluateRisk;
using System.Text.Json;

namespace RiskAssessment.Infrastructure.RuleEngine;

/// <summary>
/// Geographic rule evaluator
/// </summary>
public sealed class GeoRuleEvaluator : IRuleEvaluator
{
    private static readonly string[] HighRiskCountries = { "AF", "KP", "IR", "MM", "SD", "SY", "YE" };

    public bool CanEvaluate(RuleType ruleType) => ruleType == RuleType.GEO;

    public Task<RuleEvaluationResult> EvaluateAsync(
        RiskRule rule,
        EvaluateRiskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var conditions = JsonSerializer.Deserialize<GeoRuleConditions>(rule.ConditionsJson);
            if (conditions == null || string.IsNullOrEmpty(request.CountryCode))
            {
                return Task.FromResult(RuleEvaluationResult.NotTriggered());
            }

            var isTriggered = conditions.CheckType.ToUpperInvariant() switch
            {
                "COUNTRY_BLACKLIST" => conditions.CountryCodes.Contains(request.CountryCode),
                "COUNTRY_WHITELIST" => !conditions.CountryCodes.Contains(request.CountryCode),
                "HIGH_RISK_COUNTRY" => IsHighRiskCountry(request.CountryCode),
                _ => false
            };

            return isTriggered
                ? Task.FromResult(RuleEvaluationResult.Triggered(
                    $"Geo check triggered for country: {request.CountryCode}"))
                : Task.FromResult(RuleEvaluationResult.NotTriggered());
        }
        catch (JsonException)
        {
            return Task.FromResult(RuleEvaluationResult.NotTriggered());
        }
    }

    private static bool IsHighRiskCountry(string? countryCode)
    {
        return HighRiskCountries.Contains(countryCode?.ToUpperInvariant());
    }
}
