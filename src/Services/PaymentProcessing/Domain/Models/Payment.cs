using PaymentProcessing.Domain.Events;

namespace PaymentProcessing.Domain.Models;

/// <summary>
/// Represents a payment aggregate
/// </summary>
public sealed class Payment
{
    public Guid Id { get; private set; }
    public string PaymentId { get; private set; }
    public string MerchantId { get; private set; }
    public string CustomerId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string IdempotencyKey { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? FailureReason { get; private set; }
    public string? PaymentMethodToken { get; private set; }
    public int RetryCount { get; private set; }
    private readonly List<StateTransition> _stateTransitions = new();
    public IReadOnlyCollection<StateTransition> StateTransitions => _stateTransitions.AsReadOnly();
    private readonly List<PaymentAttempt> _attempts = new();
    public IReadOnlyCollection<PaymentAttempt> Attempts => _attempts.AsReadOnly();

    private Payment() { }

    public static Payment Create(
        string paymentId,
        string merchantId,
        string customerId,
        Money money,
        string idempotencyKey,
        string? paymentMethodToken = null)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            MerchantId = merchantId,
            CustomerId = customerId,
            Amount = money.Amount,
            Currency = money.Currency,
            Status = PaymentStatus.Created,
            IdempotencyKey = idempotencyKey,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            PaymentMethodToken = paymentMethodToken,
            RetryCount = 0
        };

        payment._stateTransitions.Add(StateTransition.Create(payment.Id, PaymentStatus.Created));
        payment.AddDomainEvent(new PaymentCreatedEvent(payment));

        return payment;
    }

    public void TransitionTo(PaymentStatus newStatus)
    {
        if (!IsValidTransition(Status, newStatus))
        {
            throw new InvalidOperationException(
                $"Invalid state transition from {Status} to {newStatus}");
        }

        var previousStatus = Status;
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        if (newStatus == PaymentStatus.Completed)
        {
            CompletedAt = DateTime.UtcNow;
        }

        _stateTransitions.Add(StateTransition.Create(Id, newStatus, previousStatus));

        AddDomainEvent(new PaymentStatusChangedEvent(this, previousStatus, newStatus));
    }

    public void RecordAttempt(string provider, AttemptStatus status, string? failureReason = null)
    {
        var attempt = PaymentAttempt.Create(
            Id,
            _attempts.Count + 1,
            provider,
            status,
            failureReason);

        _attempts.Add(attempt);

        if (status == AttemptStatus.Failed)
        {
            RetryCount++;
        }
    }

    public void MarkAsFailed(string reason)
    {
        if (Status == PaymentStatus.Completed)
        {
            throw new InvalidOperationException("Cannot mark a completed payment as failed");
        }

        TransitionTo(PaymentStatus.Failed);
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentFailedEvent(this, reason));
    }

    public void MarkAsCompleted()
    {
        TransitionTo(PaymentStatus.Completed);
        FailureReason = null;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentCompletedEvent(this));
    }

    public void Cancel()
    {
        if (Status == PaymentStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel a completed payment");
        }

        if (Status == PaymentStatus.Failed)
        {
            throw new InvalidOperationException("Cannot cancel a failed payment");
        }

        TransitionTo(PaymentStatus.Cancelled);
        AddDomainEvent(new PaymentCancelledEvent(this));
    }

    public bool CanTransitionTo(PaymentStatus newStatus)
    {
        return IsValidTransition(Status, newStatus);
    }

    public bool CanCancel =>
        Status == PaymentStatus.Created ||
        Status == PaymentStatus.Processing ||
        Status == PaymentStatus.RiskEvaluation ||
        Status == PaymentStatus.Routing ||
        Status == PaymentStatus.Authorizing;

    private static bool IsValidTransition(PaymentStatus current, PaymentStatus next)
    {
        return (current, next) switch
        {
            (PaymentStatus.Created, PaymentStatus.Processing) => true,
            (PaymentStatus.Created, PaymentStatus.Cancelled) => true,
            (PaymentStatus.Processing, PaymentStatus.RiskEvaluation) => true,
            (PaymentStatus.Processing, PaymentStatus.Cancelled) => true,
            (PaymentStatus.RiskEvaluation, PaymentStatus.Routing) => true,
            (PaymentStatus.RiskEvaluation, PaymentStatus.Failed) => true,
            (PaymentStatus.RiskEvaluation, PaymentStatus.Cancelled) => true,
            (PaymentStatus.Routing, PaymentStatus.Authorizing) => true,
            (PaymentStatus.Routing, PaymentStatus.Failed) => true,
            (PaymentStatus.Routing, PaymentStatus.Cancelled) => true,
            (PaymentStatus.Authorizing, PaymentStatus.Completed) => true,
            (PaymentStatus.Authorizing, PaymentStatus.Failed) => true,
            (PaymentStatus.Authorizing, PaymentStatus.Cancelled) => true,
            _ => false
        };
    }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
