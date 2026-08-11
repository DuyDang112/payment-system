using PaymentRouter.Domain.Models;

namespace PaymentRouter.Features.RoutePayment;

/// <summary>
/// Response from payment routing
/// </summary>
public sealed record RoutePaymentResponse(
    string DecisionId,
    string PaymentId,
    string SelectedProviderId,
    string SelectedProviderName,
    string[] AlternativeProviderIds,
    RoutingStrategy Strategy,
    string DecisionReason,
    Money CostEstimate,
    DateTime DecisionMadeAt
);
