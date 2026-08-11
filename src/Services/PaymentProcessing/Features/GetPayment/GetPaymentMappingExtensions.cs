using PaymentProcessing.Domain.Models;

namespace PaymentProcessing.Features.GetPayment;

/// <summary>
/// Mapping extensions for GetPayment feature
/// </summary>
internal static class GetPaymentMappingExtensions
{
    public static GetPaymentResponse MapToGetResponse(this Payment payment)
    {
        return new GetPaymentResponse(
            payment.PaymentId,
            payment.MerchantId,
            payment.CustomerId,
            payment.Amount,
            payment.Currency,
            payment.Status.ToString().ToUpperInvariant(),
            payment.CreatedAt,
            payment.UpdatedAt,
            payment.CompletedAt,
            payment.FailureReason,
            payment.RetryCount,
            payment.Attempts.Select(a => new PaymentAttemptResponse(
                a.AttemptNumber,
                a.Provider,
                a.Status.ToString().ToUpperInvariant(),
                a.StartedAt,
                a.CompletedAt,
                a.FailureReason
            )).ToList()
        );
    }
}
