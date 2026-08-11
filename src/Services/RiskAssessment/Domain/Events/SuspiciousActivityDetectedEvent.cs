namespace RiskAssessment.Domain.Events;

/// <summary>
/// Event that is raised when suspicious activity is detected
/// </summary>
public sealed record SuspiciousActivityDetectedEvent(
    string EvaluationId,
    string PaymentId,
    string MerchantId,
    string CustomerId,
    int RiskScore,
    List<string> SuspiciousPatterns,
    DateTime DetectedAt) : IEvent;
