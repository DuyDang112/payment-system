using RiskAssessment.Shared;

namespace RiskAssessment.Features.Shared.Errors;

/// <summary>
/// Risk assessment specific error definitions
/// </summary>
public static class RiskErrors
{
    public static readonly Error NotFound = new(
        "Risk.EvaluationNotFound",
        "Risk evaluation not found");

    public static readonly Error InvalidRiskScore = new(
        "Risk.InvalidScore",
        "Risk score must be between 0 and 100");

    public static readonly Error RuleNotFound = new(
        "Risk.RuleNotFound",
        "Risk rule not found");

    public static readonly Error MerchantProfileNotFound = new(
        "Risk.MerchantProfileNotFound",
        "Merchant risk profile not found");

    public static readonly Error InvalidRuleConditions = new(
        "Risk.InvalidConditions",
        "Invalid rule conditions");

    public static readonly Error VelocityLimitExceeded = new(
        "Risk.VelocityExceeded",
        "Velocity limit exceeded");

    public static readonly Error EntityBlacklisted = new(
        "Risk.EntityBlacklisted",
        "Entity is blacklisted");

    public static readonly Error EvaluationTimeout = new(
        "Risk.EvaluationTimeout",
        "Risk evaluation timed out");

    public static readonly Error WhitelistedEntity = new(
        "Risk.WhitelistedEntity",
        "Entity is whitelisted and bypasses checks");

    public static Error EvaluationNotFoundWithId(string evaluationId) => new(
        "Risk.EvaluationNotFound",
        $"Risk evaluation '{evaluationId}' not found");

    public static Error RuleNotFoundWithId(string ruleId) => new(
        "Risk.RuleNotFound",
        $"Risk rule '{ruleId}' not found");

    public static Error MerchantProfileNotFoundWithId(string merchantId) => new(
        "Risk.MerchantProfileNotFound",
        $"Merchant risk profile for '{merchantId}' not found");

    public static Error InvalidRuleConditionWithMessage(string message) => new(
        "Risk.InvalidConditions",
        $"Invalid rule conditions: {message}");
}
