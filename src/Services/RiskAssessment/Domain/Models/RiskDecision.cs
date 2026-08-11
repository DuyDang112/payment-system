namespace RiskAssessment.Domain.Models;

/// <summary>
/// Risk decision types
/// </summary>
public enum RiskDecision
{
    /// <summary>
    /// Transaction is approved
    /// </summary>
    APPROVE,

    /// <summary>
    /// Transaction is rejected
    /// </summary>
    REJECT,

    /// <summary>
    /// Transaction requires manual review
    /// </summary>
    REVIEW
}
