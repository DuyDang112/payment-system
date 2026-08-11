namespace PaymentProcessing.Domain.Models;

/// <summary>
/// Represents a state transition for audit trail
/// </summary>
public sealed class StateTransition
{
    public Guid Id { get; private set; }
    public Guid PaymentId { get; private set; }
    public PaymentStatus FromStatus { get; private set; }
    public PaymentStatus ToStatus { get; private set; }
    public DateTime TransitionedAt { get; private set; }

    private StateTransition() { }

    public static StateTransition Create(
        Guid paymentId,
        PaymentStatus toStatus,
        PaymentStatus? fromStatus = null)
    {
        return new StateTransition
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            FromStatus = fromStatus ?? toStatus,
            ToStatus = toStatus,
            TransitionedAt = DateTime.UtcNow
        };
    }
}
