namespace PaymentRouter.Domain.Models;

/// <summary>
/// Routing decision entity
/// </summary>
public sealed class RoutingDecision
{
    public string DecisionId { get; private set; }
    public string PaymentId { get; private set; }
    public string MerchantId { get; private set; }
    public string SelectedProviderId { get; private set; }
    public string[] AlternativeProviderIds { get; private set; }
    public RoutingStrategy Strategy { get; private set; }
    public string DecisionReason { get; private set; }
    public string CostEstimateJson { get; private set; }
    public DateTime DecisionMadeAt { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }

    private RoutingDecision() { }

    public static RoutingDecision Create(
        string paymentId,
        string merchantId,
        string selectedProviderId,
        string[] alternativeProviderIds,
        RoutingStrategy strategy,
        string decisionReason,
        Money costEstimate,
        Money amount,
        PaymentMethod paymentMethod)
    {
        return new RoutingDecision
        {
            DecisionId = Guid.NewGuid().ToString("N"),
            PaymentId = paymentId,
            MerchantId = merchantId,
            SelectedProviderId = selectedProviderId,
            AlternativeProviderIds = alternativeProviderIds,
            Strategy = strategy,
            DecisionReason = decisionReason,
            CostEstimateJson = System.Text.Json.JsonSerializer.Serialize(costEstimate),
            DecisionMadeAt = DateTime.UtcNow,
            Amount = amount.Amount,
            Currency = amount.Currency,
            PaymentMethod = paymentMethod
        };
    }

    public Money GetCostEstimate()
    {
        return System.Text.Json.JsonSerializer.Deserialize<Money>(CostEstimateJson)
            ?? Money.Zero("USD");
    }

    public Money GetAmount() => new Money(Amount, Currency);
}
