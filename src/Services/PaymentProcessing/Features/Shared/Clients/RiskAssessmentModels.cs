namespace PaymentProcessing.Features.Shared.Clients;

/// <summary>
/// Request models for Risk Assessment service
/// </summary>
public sealed record EvaluateRiskRequest
{
    public string PaymentId { get; init; }
    public string MerchantId { get; init; }
    public string CustomerId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; }
    public string? CountryCode { get; init; }
    public string? IpAddress { get; init; }
    public string? CustomerEmail { get; init; }
}

/// <summary>
/// Response models from Risk Assessment service
/// </summary>
public sealed record EvaluateRiskResponse
{
    public string EvaluationId { get; init; }
    public string PaymentId { get; init; }
    public int RiskScore { get; init; }
    public string Decision { get; init; }
    public DateTime EvaluatedAt { get; init; }
    public List<TriggeredRule>? TriggeredRules { get; init; }
}

public sealed record TriggeredRule
{
    public string RuleId { get; init; }
    public string RuleName { get; init; }
    public string Action { get; init; }
    public int ScoreImpact { get; init; }
    public string Description { get; init; }
}