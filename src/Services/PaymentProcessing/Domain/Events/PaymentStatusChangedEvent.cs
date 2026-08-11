using PaymentProcessing.Domain.Models;

namespace PaymentProcessing.Domain.Events;

/// <summary>
/// Event raised when payment status changes
/// </summary>
public sealed record PaymentStatusChangedEvent(
    Payment Payment,
    PaymentStatus PreviousStatus,
    PaymentStatus NewStatus
) : IDomainEvent;
