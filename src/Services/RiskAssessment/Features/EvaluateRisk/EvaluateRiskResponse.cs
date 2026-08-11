using RiskAssessment.Domain.Models;

namespace RiskAssessment.Features.EvaluateRisk;

/// <summary>
/// Response from risk evaluation
/// </summary>
public sealed record EvaluateRiskResponse
{
    public string EvaluationId { get; init; } = string.Empty;
    public string PaymentId { get; init; } = string.Empty;
    public int RiskScore { get; init; }
    public string Decision { get; init; } = string.Empty;
    public DateTime EvaluatedAt { get; init; }
    public string RuleVersion { get; init; } = string.Empty;
    public List<TriggeredRuleDto> TriggeredRules { get; init; } = new();
    public bool TimeoutOccurred { get; init; }
    public string? Warning { get; init; }
    public RiskEvaluationDetailsDto? EvaluationDetails { get; init; }
}

/// <summary>
/// Triggered rule DTO
/// </summary>
public sealed record TriggeredRuleDto
{
    public string RuleId { get; init; } = string.Empty;
    public string RuleName { get; init; } = string.Empty;
    public string RuleType { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public int ScoreImpact { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Risk evaluation details DTO
/// </summary>
public sealed record RiskEvaluationDetailsDto
{
    public VelocityCheckResultDto? VelocityChecks { get; init; }
    public bool BlacklistCheck { get; init; }
    public bool WhitelistCheck { get; init; }
    public GeoLocationCheckResultDto? GeoLocationCheck { get; init; }
    public AmountCheckResultDto? AmountCheck { get; init; }
}

/// <summary>
/// Velocity check result DTO
/// </summary>
public sealed record VelocityCheckResultDto
{
    public string WindowType { get; init; } = string.Empty;
    public int Count { get; init; }
    public int Limit { get; init; }
    public bool IsExceeded { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// Geographic location check result DTO
/// </summary>
public sealed record GeoLocationCheckResultDto
{
    public string CountryCode { get; init; } = string.Empty;
    public bool IsHighRiskCountry { get; init; }
    public bool IsWhitelisted { get; init; }
    public bool IsBlacklisted { get; init; }
}

/// <summary>
/// Amount check result DTO
/// </summary>
public sealed record AmountCheckResultDto
{
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
    public bool IsAboveThreshold { get; init; }
}
