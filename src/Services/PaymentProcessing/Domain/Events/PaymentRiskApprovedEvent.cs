using PaymentProcessing.Domain.Models;

namespace PaymentProcessing.Domain.Events;

/// <summary>
/// Event raised when payment passes risk evaluation
/// </summary>
public sealed record PaymentRiskApprovedEvent(Payment Payment) : IEvent;
