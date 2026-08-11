namespace PaymentProcessing.Features.GetPayment;

/// <summary>
/// Response DTO for getting a payment
/// </summary>
public sealed record GetPaymentResponse(
    string PaymentId,
    string MerchantId,
    string CustomerId,
    decimal Amount,
    string Currency,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt,
    string? FailureReason,
    int RetryCount,
    List<PaymentAttemptResponse> Attempts
);

/// <summary>
/// Response DTO for payment attempt
/// </summary>
public sealed record PaymentAttemptResponse(
    int AttemptNumber,
    string Provider,
    string Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string? FailureReason
);
