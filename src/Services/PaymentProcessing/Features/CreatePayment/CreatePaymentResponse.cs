namespace PaymentProcessing.Features.CreatePayment;

/// <summary>
/// Response DTO for created payment
/// </summary>
public sealed record CreatePaymentResponse(
    string PaymentId,
    string MerchantId,
    string CustomerId,
    decimal Amount,
    string Currency,
    string Status,
    DateTime CreatedAt,
    Dictionary<string, string>? Metadata
);
