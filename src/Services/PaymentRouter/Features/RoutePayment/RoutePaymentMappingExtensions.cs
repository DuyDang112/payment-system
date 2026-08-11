using PaymentRouter.Domain.Models;
using PaymentRouter.Infrastructure.RoutingEngine;

namespace PaymentRouter.Features.RoutePayment;

internal static class RoutePaymentMappingExtensions
{
    public static RoutingContext ToRoutingContext(this RoutePaymentRequest request)
    {
        return new RoutingContext(
            request.MerchantId,
            request.PaymentId,
            new Money(request.Amount, request.Currency),
            request.Currency,
            request.PaymentMethod,
            request.CountryCode);
    }
}
