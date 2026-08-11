using PaymentProcessing.Shared;

namespace PaymentProcessing.Features.CancelPayment;

/// <summary>
/// Interface for CancelPayment handler
/// </summary>
public interface ICancelPaymentHandler : IHandler
{
    Task<Result<CancelPaymentResponse>> HandleAsync(
        string paymentId,
        CancelPaymentRequest request,
        CancellationToken cancellationToken);
}
