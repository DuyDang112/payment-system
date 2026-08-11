using PaymentProcessing.Shared;

namespace PaymentProcessing.Features.CreatePayment;

/// <summary>
/// Interface for CreatePayment handler
/// </summary>
public interface ICreatePaymentHandler : IHandler
{
    Task<Result<CreatePaymentResponse>> HandleAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken);
}
