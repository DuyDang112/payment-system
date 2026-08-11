using PaymentRouter.Domain.Models;

namespace PaymentRouter.Features.RoutePayment;

/// <summary>
/// Request to route a payment to the best provider
/// </summary>
public sealed record RoutePaymentRequest(
    string PaymentId,
    string MerchantId,
    decimal Amount,
    string Currency,
    PaymentMethod PaymentMethod,
    string? CountryCode = null,
    RoutingStrategy Strategy = RoutingStrategy.COST_BASED
);
