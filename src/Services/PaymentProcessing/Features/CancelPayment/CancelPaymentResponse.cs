namespace PaymentProcessing.Features.CancelPayment;

/// <summary>
/// Response DTO for cancelled payment
/// </summary>
public sealed record CancelPaymentResponse(
    string PaymentId,
    string Status,
    DateTime CancelledAt
);
