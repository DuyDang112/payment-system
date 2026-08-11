using PaymentRouter.Domain.Models;
using PaymentRouter.Infrastructure.CircuitBreaking;

namespace PaymentRouter.Infrastructure.RoutingEngine;

/// <summary>
/// Main routing engine that coordinates provider selection and routing decisions
/// </summary>
public interface IRoutingEngine
{
    Task<RoutingDecision> MakeRoutingDecisionAsync(
        RoutingContext context,
        RoutingStrategy strategy,
        CancellationToken cancellationToken);
}

public sealed class RoutingEngine : IRoutingEngine
{
    private readonly IProviderSelector _providerSelector;
    private readonly ICostCalculator _costCalculator;
    private readonly ICircuitBreakerManager _circuitBreakerManager;
    private readonly ILogger<RoutingEngine> _logger;

    public RoutingEngine(
        IProviderSelector providerSelector,
        ICostCalculator costCalculator,
        ICircuitBreakerManager circuitBreakerManager,
        ILogger<RoutingEngine> logger)
    {
        _providerSelector = providerSelector;
        _costCalculator = costCalculator;
        _circuitBreakerManager = circuitBreakerManager;
        _logger = logger;
    }

    public async Task<RoutingDecision> MakeRoutingDecisionAsync(
        RoutingContext context,
        RoutingStrategy strategy,
        CancellationToken cancellationToken)
    {
        var selectionResult = await _providerSelector.SelectProviderAsync(context, cancellationToken);

        if (selectionResult.SelectedProvider == null)
        {
            throw new InvalidOperationException("No eligible providers available");
        }

        var costEstimate = _costCalculator.CalculateCost(selectionResult.SelectedProvider, context.Amount);

        var decision = RoutingDecision.Create(
            context.PaymentId,
            context.MerchantId,
            selectionResult.SelectedProvider.ProviderId,
            selectionResult.AlternativeProviders,
            strategy,
            selectionResult.Reason,
            costEstimate,
            context.Amount,
            context.PaymentMethod);

        _logger.LogInformation(
            "Routing decision {DecisionId} made for payment {PaymentId}: Provider {ProviderId}, Cost {Cost}",
            decision.DecisionId,
            context.PaymentId,
            decision.SelectedProviderId,
            costEstimate.Amount);

        return decision;
    }
}
