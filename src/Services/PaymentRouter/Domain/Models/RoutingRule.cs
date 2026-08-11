namespace PaymentRouter.Domain.Models;

/// <summary>
/// Routing rule for provider selection
/// </summary>
public sealed class RoutingRule
{
    public string RuleId { get; private set; }
    public string MerchantId { get; private set; }
    public string Name { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }
    public string ConditionsJson { get; private set; }
    public string[] PreferredProviderIds { get; private set; }
    public RoutingStrategy Strategy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private RoutingRule() { }

    public static RoutingRule Create(
        string merchantId,
        string name,
        int priority,
        RoutingRuleCondition conditions,
        string[] preferredProviderIds,
        RoutingStrategy strategy)
    {
        return new RoutingRule
        {
            RuleId = Guid.NewGuid().ToString("N"),
            MerchantId = merchantId,
            Name = name,
            Priority = priority,
            IsActive = true,
            ConditionsJson = System.Text.Json.JsonSerializer.Serialize(conditions),
            PreferredProviderIds = preferredProviderIds,
            Strategy = strategy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public RoutingRuleCondition GetConditions()
    {
        return System.Text.Json.JsonSerializer.Deserialize<RoutingRuleCondition>(ConditionsJson)
            ?? new RoutingRuleCondition();
    }

    public void UpdateConditions(RoutingRuleCondition conditions)
    {
        ConditionsJson = System.Text.Json.JsonSerializer.Serialize(conditions);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Conditions for routing rules
/// </summary>
public sealed record RoutingRuleCondition
{
    public string[]? Currencies { get; init; }
    public PaymentMethod[]? PaymentMethods { get; init; }
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public string[]? Countries { get; init; }

    public bool Matches(
        string currency,
        PaymentMethod paymentMethod,
        decimal amount,
        string? countryCode = null)
    {
        if (Currencies != null && Currencies.Length > 0 && !Currencies.Contains(currency))
            return false;

        if (PaymentMethods != null && PaymentMethods.Length > 0 && !PaymentMethods.Contains(paymentMethod))
            return false;

        if (MinAmount.HasValue && amount < MinAmount.Value)
            return false;

        if (MaxAmount.HasValue && amount > MaxAmount.Value)
            return false;

        if (Countries != null && Countries.Length > 0 && countryCode != null && !Countries.Contains(countryCode))
            return false;

        return true;
    }
}
