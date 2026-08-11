using PaymentProcessing.Domain.Models;

namespace PaymentProcessing.Domain.Events;

/// <summary>
/// Event raised when a payment is created
/// </summary>
public sealed record PaymentCreatedEvent(Payment Payment) : IDomainEvent, IEvent;
