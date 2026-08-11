using PaymentProcessing.Domain.Models;

namespace PaymentProcessing.Domain.Events;

/// <summary>
/// Event raised when payment fails risk evaluation
/// </summary>
public sealed record PaymentRiskRejectedEvent(Payment Payment, string Reason) : IEvent;
