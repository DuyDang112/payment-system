namespace PaymentProcessing.Features.CancelPayment;

/// <summary>
/// Request DTO for cancelling a payment
/// </summary>
public sealed record CancelPaymentRequest(
    string Reason
);
