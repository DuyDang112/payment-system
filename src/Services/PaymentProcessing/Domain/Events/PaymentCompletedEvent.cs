using PaymentProcessing.Domain.Models;

namespace PaymentProcessing.Domain.Events;

/// <summary>
/// Event raised when a payment is completed
/// </summary>
public sealed record PaymentCompletedEvent(Payment Payment) : IDomainEvent, IEvent;
