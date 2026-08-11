namespace PaymentProcessing.Features.Shared.Clients;

/// <summary>
/// Request models for Payment Router service
/// </summary>
public sealed record RoutePaymentRequest
{
    public string PaymentId { get; init; }
    public string MerchantId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; }
    public string PaymentMethod { get; init; }
    public string? CountryCode { get; init; }
    public string Strategy { get; init; }
}

/// <summary>
/// Response models from Payment Router service
/// </summary>
public sealed record RoutePaymentResponse
{
    public string DecisionId { get; init; }
    public string PaymentId { get; init; }
    public string SelectedProviderId { get; init; }
    public string ProviderName { get; init; }
    public List<string> AlternativeProviderIds { get; init; }
    public string Strategy { get; init; }
    public string DecisionReason { get; init; }
    public CostEstimate CostEstimate { get; init; }
}

public sealed record CostEstimate
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }
}