namespace BankAdapter.Features.Refund;

/// <summary>
/// Request for payment refund
/// </summary>
public sealed record RefundRequest(
    string PaymentId,
    string ProviderId,
    string AuthorizationToken,
    decimal Amount,
    string Currency,
    Dictionary<string, string> Metadata
);

/// <summary>
/// Response for payment refund
/// </summary>
public sealed record RefundResponse(
    string PaymentId,
    string ProviderId,
    string ProviderTransactionId,
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    Dictionary<string, string>? Metadata = null
);
