using System.Text.Json.Serialization;

namespace RiskAssessment.Infrastructure.RuleEngine;

/// <summary>
/// Velocity rule conditions
/// </summary>
public sealed record VelocityRuleConditions
{
    [JsonPropertyName("windowType")]
    public string WindowType { get; init; } = string.Empty;

    [JsonPropertyName("limit")]
    public int Limit { get; init; }
}

/// <summary>
/// Blacklist rule conditions
/// </summary>
public sealed record BlacklistRuleConditions
{
    [JsonPropertyName("entityType")]
    public string EntityType { get; init; } = string.Empty;

    [JsonPropertyName("entityValue")]
    public string? EntityValue { get; init; }
}

/// <summary>
/// Amount rule conditions
/// </summary>
public sealed record AmountRuleConditions
{
    [JsonPropertyName("operator")]
    public string Operator { get; init; } = string.Empty;

    [JsonPropertyName("minAmount")]
    public decimal? MinAmount { get; init; }

    [JsonPropertyName("maxAmount")]
    public decimal? MaxAmount { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }
}

/// <summary>
/// Geographic rule conditions
/// </summary>
public sealed record GeoRuleConditions
{
    [JsonPropertyName("checkType")]
    public string CheckType { get; init; } = string.Empty;

    [JsonPropertyName("countryCodes")]
    public List<string> CountryCodes { get; init; } = new();
}

/// <summary>
/// Rule evaluation result
/// </summary>
public sealed record RuleEvaluationResult(
    bool IsTriggered,
    string? Message = null)
{
    public static RuleEvaluationResult Triggered(string message)
        => new(true, message);

    public static RuleEvaluationResult NotTriggered()
        => new(false);
}
