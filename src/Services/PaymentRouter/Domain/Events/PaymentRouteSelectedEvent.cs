using PaymentRouter.Domain.Models;

namespace PaymentRouter.Domain.Events;

/// <summary>
/// Event published when a payment route is selected
/// </summary>
public sealed record PaymentRouteSelectedEvent(
    string DecisionId,
    string PaymentId,
    string SelectedProviderId,
    string[] AlternativeProviderIds,
    RoutingStrategy Strategy,
    Money CostEstimate,
    DateTime DecisionMadeAt
) : IEvent;
