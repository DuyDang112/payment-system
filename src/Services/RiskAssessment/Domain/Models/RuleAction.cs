namespace RiskAssessment.Domain.Models;

/// <summary>
/// Rule action types
/// </summary>
public enum RuleAction
{
    /// <summary>
    /// Block the transaction immediately
    /// </summary>
    BLOCK,

    /// <summary>
    /// Flag the transaction for review
    /// </summary>
    FLAG,

    /// <summary>
    /// Add score impact to risk score
    /// </summary>
    ADD_SCORE
}
