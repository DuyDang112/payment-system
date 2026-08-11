namespace RiskAssessment.Features.EvaluateRisk;

/// <summary>
/// Request for risk evaluation
/// </summary>
public sealed record EvaluateRiskRequest
{
    public string PaymentId { get; init; } = string.Empty;
    public string MerchantId { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
    public string? CountryCode { get; init; }
    public string? IpAddress { get; init; }
    public string? PaymentMethodToken { get; init; }
}
