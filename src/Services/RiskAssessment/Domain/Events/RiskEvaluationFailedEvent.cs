namespace RiskAssessment.Domain.Events;

/// <summary>
/// Event that is raised when a risk evaluation fails
/// </summary>
public sealed record RiskEvaluationFailedEvent(
    string PaymentId,
    string MerchantId,
    string Reason,
    DateTime FailedAt) : IDomainEvent;
