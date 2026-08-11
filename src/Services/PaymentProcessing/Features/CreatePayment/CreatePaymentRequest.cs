using Shared.Domains;

namespace PaymentProcessing.Features.CreatePayment;

/// <summary>
/// Request DTO for creating a payment
/// </summary>
public sealed record CreatePaymentRequest(
    string MerchantId,
    string CustomerId,
    decimal Amount,
    string Currency,
    string IdempotencyKey,
    string PaymentMethod,
    string? PaymentMethodToken,
    Dictionary<string, string>? Metadata
);
