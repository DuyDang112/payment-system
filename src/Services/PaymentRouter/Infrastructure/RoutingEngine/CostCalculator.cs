using PaymentRouter.Domain.Models;

namespace PaymentRouter.Infrastructure.RoutingEngine;

/// <summary>
/// Calculates processing costs for payment providers
/// </summary>
public interface ICostCalculator
{
    Money CalculateCost(PaymentProvider provider, Money amount);
    Money[] EstimateCosts(IEnumerable<PaymentProvider> providers, Money amount);
}

public sealed class CostCalculator : ICostCalculator
{
    public Money CalculateCost(PaymentProvider provider, Money amount)
    {
        var costConfig = provider.GetCostConfiguration();
        return costConfig.CalculateCost(amount);
    }

    public Money[] EstimateCosts(IEnumerable<PaymentProvider> providers, Money amount)
    {
        return providers.Select(p => CalculateCost(p, amount)).ToArray();
    }
}
