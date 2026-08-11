namespace BankAdapter.Features.Void;

/// <summary>
/// Request for payment void
/// </summary>
public sealed record VoidRequest(
    string PaymentId,
    string ProviderId,
    string AuthorizationToken,
    decimal Amount,
    string Currency,
    Dictionary<string, string> Metadata
);

/// <summary>
/// Response for payment void
/// </summary>
public sealed record VoidResponse(
    string PaymentId,
    string ProviderId,
    string ProviderTransactionId,
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    Dictionary<string, string>? Metadata = null
);
