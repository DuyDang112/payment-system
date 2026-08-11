namespace RiskAssessment.Domain.Models;

/// <summary>
/// Rule type definitions
/// </summary>
public enum RuleType
{
    /// <summary>
    /// Velocity-based rule
    /// </summary>
    VELOCITY,

    /// <summary>
    /// Blacklist check rule
    /// </summary>
    BLACKLIST,

    /// <summary>
    /// Amount-based rule
    /// </summary>
    AMOUNT,

    /// <summary>
    /// Geographic location rule
    /// </summary>
    GEO,

    /// <summary>
    /// Machine learning model rule
    /// </summary>
    ML_MODEL
}
