using PaymentProcessing.Domain.Models;

namespace PaymentProcessing.Features.CreatePayment;

/// <summary>
/// Mapping extensions for CreatePayment feature
/// </summary>
internal static class CreatePaymentMappingExtensions
{
    public static CreatePaymentResponse MapToCreateResponse(this Payment payment)
    {
        return new CreatePaymentResponse(
            payment.PaymentId,
            payment.MerchantId,
            payment.CustomerId,
            payment.Amount,
            payment.Currency,
            payment.Status.ToString().ToUpperInvariant(),
            payment.CreatedAt,
            new Dictionary<string, string>()
        );
    }
}
