namespace PaymentProcessing.Domain.Models;

/// <summary>
/// Represents a payment attempt for retry tracking
/// </summary>
public sealed class PaymentAttempt
{
    public Guid Id { get; private set; }
    public Guid PaymentId { get; private set; }
    public int AttemptNumber { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public AttemptStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public string Provider { get; private set; }

    private PaymentAttempt() { }

    public static PaymentAttempt Create(
        Guid paymentId,
        int attemptNumber,
        string provider,
        AttemptStatus status = AttemptStatus.Started,
        string? failureReason = null)
    {
        return new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            AttemptNumber = attemptNumber,
            Provider = provider,
            Status = status,
            StartedAt = DateTime.UtcNow,
            CompletedAt = status == AttemptStatus.Completed || status == AttemptStatus.Failed
                ? DateTime.UtcNow
                : null,
            FailureReason = failureReason
        };
    }

    public void MarkAsCompleted()
    {
        Status = AttemptStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string reason)
    {
        Status = AttemptStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        FailureReason = reason;
    }
}
