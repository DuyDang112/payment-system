namespace RiskAssessment.Domain.Models;

/// <summary>
/// Velocity window types for rate limiting
/// </summary>
public enum VelocityWindow
{
    /// <summary>
    /// One minute window
    /// </summary>
    ONE_MINUTE,

    /// <summary>
    /// Five minute window
    /// </summary>
    FIVE_MINUTES,

    /// <summary>
    /// One hour window
    /// </summary>
    ONE_HOUR,

    /// <summary>
    /// One day window
    /// </summary>
    ONE_DAY
}
