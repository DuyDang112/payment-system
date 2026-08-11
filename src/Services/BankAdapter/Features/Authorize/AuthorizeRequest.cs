namespace BankAdapter.Features.Authorize;

/// <summary>
/// Request for payment authorization
/// </summary>
public sealed record AuthorizeRequest(
    string PaymentId,
    string ProviderId,
    decimal Amount,
    string Currency,
    Dictionary<string, string> Metadata
);

/// <summary>
/// Response for payment authorization
/// </summary>
public sealed record AuthorizeResponse(
    string PaymentId,
    string ProviderId,
    string ProviderTransactionId,
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    Dictionary<string, string>? Metadata = null
);
