using RiskAssessment.Domain.Models;

namespace RiskAssessment.Domain.Events;

/// <summary>
/// Event that is raised when a risk evaluation is completed
/// </summary>
public sealed record RiskEvaluationCompletedEvent(
    string EvaluationId,
    string PaymentId,
    string MerchantId,
    int RiskScore,
    RiskDecision Decision,
    DateTime EvaluatedAt) : IDomainEvent;
