namespace BankAdapter.Domain;

/// <summary>
/// Represents a log entry for provider requests
/// </summary>
public sealed class ProviderRequestLog
{
    public string LogId { get; private set; } = string.Empty;
    public string PaymentId { get; private set; } = string.Empty;
    public string ProviderId { get; private set; } = string.Empty;
    public Operation Operation { get; private set; }
    public DateTime RequestSentAt { get; private set; }
    public DateTime? ResponseReceivedAt { get; private set; }
    public int? Duration { get; private set; }
    public string RequestPayload { get; private set; } = string.Empty;
    public string ResponsePayload { get; private set; } = string.Empty;
    public int? HttpStatusCode { get; private set; }
    public ProviderResult Result { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// EF Core constructor
    /// </summary>
    private ProviderRequestLog() { }

    /// <summary>
    /// Create a new request log entry
    /// </summary>
    public static ProviderRequestLog Create(
        string paymentId,
        string providerId,
        Operation operation,
        string requestPayload)
    {
        return new ProviderRequestLog
        {
            LogId = Guid.NewGuid().ToString(),
            PaymentId = paymentId,
            ProviderId = providerId,
            Operation = operation,
            RequestSentAt = DateTime.UtcNow,
            RequestPayload = requestPayload,
            Result = ProviderResult.RetryableError,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Mark request as completed successfully
    /// </summary>
    public void CompleteSuccess(string responsePayload, int httpStatusCode)
    {
        ResponseReceivedAt = DateTime.UtcNow;
        Duration = (int)(ResponseReceivedAt.Value - RequestSentAt).TotalMilliseconds;
        ResponsePayload = responsePayload;
        HttpStatusCode = httpStatusCode;
        Result = ProviderResult.Success;
    }

    /// <summary>
    /// Mark request as completed with error
    /// </summary>
    public void CompleteError(
        string responsePayload,
        int httpStatusCode,
        string? errorCode = null,
        string? errorMessage = null,
        ProviderResult result = ProviderResult.FatalError)
    {
        ResponseReceivedAt = DateTime.UtcNow;
        Duration = (int)(ResponseReceivedAt.Value - RequestSentAt).TotalMilliseconds;
        ResponsePayload = responsePayload;
        HttpStatusCode = httpStatusCode;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Result = result;
    }

    /// <summary>
    /// Increment retry count
    /// </summary>
    public void IncrementRetry()
    {
        RetryCount++;
    }
}
