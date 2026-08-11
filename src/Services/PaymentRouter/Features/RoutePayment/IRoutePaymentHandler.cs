using PaymentRouter.Shared;

namespace PaymentRouter.Features.RoutePayment;

internal interface IRoutePaymentHandler : IHandler
{
    Task<Result<RoutePaymentResponse>> HandleAsync(RoutePaymentRequest request, CancellationToken cancellationToken);
}
