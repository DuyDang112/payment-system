namespace RiskAssessment.Domain.Models;

/// <summary>
/// Risk score value object (0-100)
/// </summary>
public sealed record RiskScore(int Value)
{
    public static readonly RiskScore Low = new(0);
    public static readonly RiskScore Medium = new(50);
    public static readonly RiskScore High = new(80);

    public static RiskScore Of(int value)
    {
        if (value < 0 || value > 100)
            throw new ArgumentException("Risk score must be between 0 and 100", nameof(value));

        return new RiskScore(value);
    }

    public bool IsLowRisk => Value < 50;
    public bool IsMediumRisk => Value >= 50 && Value < 80;
    public bool IsHighRisk => Value >= 80;
}
