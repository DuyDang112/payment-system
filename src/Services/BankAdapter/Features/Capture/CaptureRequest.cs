namespace BankAdapter.Features.Capture;

/// <summary>
/// Request for payment capture
/// </summary>
public sealed record CaptureRequest(
    string PaymentId,
    string ProviderId,
    string AuthorizationToken,
    decimal Amount,
    string Currency,
    Dictionary<string, string> Metadata
);

/// <summary>
/// Response for payment capture
/// </summary>
public sealed record CaptureResponse(
    string PaymentId,
    string ProviderId,
    string ProviderTransactionId,
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    Dictionary<string, string>? Metadata = null
);
