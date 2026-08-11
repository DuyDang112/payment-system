namespace RiskAssessment.Domain.Models;

/// <summary>
/// Result of velocity check
/// </summary>
public sealed record VelocityCheckResult(
    bool IsExceeded,
    string WindowType,
    int Count,
    int Limit,
    string? Message)
{
    public static VelocityCheckResult NotExceeded()
        => new(false, "NONE", 0, 0, null);

    public static VelocityCheckResult Exceeded(string window, int count, int limit)
        => new(true, window, count, limit, $"Velocity limit exceeded for {window}: {count}/{limit}");
}
