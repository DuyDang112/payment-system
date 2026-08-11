namespace BankAdapter.Infrastructure.Events;

/// <summary>
/// Base interface for all events
/// </summary>
public interface IEvent
{
    string EventType { get; }
    DateTime OccurredAt { get; }
}

/// <summary>
/// Event raised when payment authorization succeeds
/// </summary>
public sealed record PaymentAuthorizationSucceededEvent(
    string PaymentId,
    string ProviderId,
    string ProviderTransactionId,
    decimal Amount,
    string Currency,
    Dictionary<string, string> Metadata,
    DateTime OccurredAt
) : IEvent
{
    public string EventType => "PaymentAuthorizationSucceeded";
}

/// <summary>
/// Event raised when payment authorization fails
/// </summary>
public sealed record PaymentAuthorizationFailedEvent(
    string PaymentId,
    string ProviderId,
    string ErrorCode,
    string ErrorMessage,
    decimal Amount,
    string Currency,
    Dictionary<string, string> Metadata,
    DateTime OccurredAt
) : IEvent
{
    public string EventType => "PaymentAuthorizationFailed";
}

/// <summary>
/// Event raised when payment capture succeeds
/// </summary>
public sealed record PaymentCaptureSucceededEvent(
    string PaymentId,
    string ProviderId,
    string ProviderTransactionId,
    decimal Amount,
    string Currency,
    Dictionary<string, string> Metadata,
    DateTime OccurredAt
) : IEvent
{
    public string EventType => "PaymentCaptureSucceeded";
}

/// <summary>
/// Event raised when payment capture fails
/// </summary>
public sealed record PaymentCaptureFailedEvent(
    string PaymentId,
    string ProviderId,
    string ErrorCode,
    string ErrorMessage,
    decimal Amount,
    string Currency,
    Dictionary<string, string> Metadata,
    DateTime OccurredAt
) : IEvent
{
    public string EventType => "PaymentCaptureFailed";
}

/// <summary>
/// Event raised when payment refund succeeds
/// </summary>
public sealed record PaymentRefundSucceededEvent(
    string PaymentId,
    string ProviderId,
    string ProviderTransactionId,
    decimal Amount,
    string Currency,
    Dictionary<string, string> Metadata,
    DateTime OccurredAt
) : IEvent
{
    public string EventType => "PaymentRefundSucceeded";
}

/// <summary>
/// Event raised when payment refund fails
/// </summary>
public sealed record PaymentRefundFailedEvent(
    string PaymentId,
    string ProviderId,
    string ErrorCode,
    string ErrorMessage,
    decimal Amount,
    string Currency,
    Dictionary<string, string> Metadata,
    DateTime OccurredAt
) : IEvent
{
    public string EventType => "PaymentRefundFailed";
}

/// <summary>
/// Event raised when provider rate limit is exceeded
/// </summary>
public sealed record ProviderRateLimitExceededEvent(
    string ProviderId,
    int CurrentRequests,
    int MaxRequests,
    DateTime OccurredAt
) : IEvent
{
    public string EventType => "ProviderRateLimitExceeded";
}

/// <summary>
/// Event raised when provider health changes
/// </summary>
public sealed record ProviderHealthChangedEvent(
    string ProviderId,
    bool IsHealthy,
    string? Message,
    DateTime OccurredAt
) : IEvent
{
    public string EventType => "ProviderHealthChanged";
}
