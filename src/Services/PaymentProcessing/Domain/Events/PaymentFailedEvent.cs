using PaymentProcessing.Domain.Models;

namespace PaymentProcessing.Domain.Events;

/// <summary>
/// Event raised when a payment fails
/// </summary>
public sealed record PaymentFailedEvent(Payment Payment, string Reason) : IDomainEvent, IEvent;
