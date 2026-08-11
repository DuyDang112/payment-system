using PaymentProcessing.Domain.Models;

namespace PaymentProcessing.Domain.Events;

/// <summary>
/// Event raised when a payment is cancelled
/// </summary>
public sealed record PaymentCancelledEvent(Payment Payment) : IDomainEvent, IEvent;
