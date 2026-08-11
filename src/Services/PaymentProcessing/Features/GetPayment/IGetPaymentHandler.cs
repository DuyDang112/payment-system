using PaymentProcessing.Shared;

namespace PaymentProcessing.Features.GetPayment;

/// <summary>
/// Interface for GetPayment handler
/// </summary>
public interface IGetPaymentHandler : IHandler
{
    Task<Result<GetPaymentResponse>> HandleAsync(
        string paymentId,
        CancellationToken cancellationToken);
}
