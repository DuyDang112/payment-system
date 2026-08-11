namespace PaymentRouter.Domain.Models;

/// <summary>
/// Routing strategies for provider selection
/// </summary>
public enum RoutingStrategy
{
    COST_BASED,
    PERFORMANCE,
    PRIORITY,
    ROUND_ROBIN
}
