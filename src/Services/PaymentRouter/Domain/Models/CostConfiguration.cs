namespace PaymentRouter.Domain.Models;

/// <summary>
/// Cost configuration for payment provider fees
/// </summary>
public sealed record CostConfiguration
{
    public Money FixedFee { get; init; }
    public decimal PercentageFee { get; init; }
    public Money? MinFee { get; init; }
    public Money? MaxFee { get; init; }

    public CostConfiguration(Money fixedFee, decimal percentageFee, Money? minFee = null, Money? maxFee = null)
    {
        FixedFee = fixedFee;
        PercentageFee = percentageFee;
        MinFee = minFee;
        MaxFee = maxFee;
    }

    /// <summary>
    /// Calculate cost for a given amount
    /// </summary>
    public Money CalculateCost(Money amount)
    {
        var variableFee = amount.Multiply(PercentageFee / 100);
        var totalFee = FixedFee.Add(variableFee);

        if (MinFee != null && totalFee.Amount < MinFee.Amount)
            return MinFee;

        if (MaxFee != null && totalFee.Amount > MaxFee.Amount)
            return MaxFee;

        return totalFee;
    }
}
