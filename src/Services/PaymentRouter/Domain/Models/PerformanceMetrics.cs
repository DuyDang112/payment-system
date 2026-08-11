namespace PaymentRouter.Domain.Models;

/// <summary>
/// Performance metrics for payment providers
/// </summary>
public sealed record PerformanceMetrics
{
    public double SuccessRate { get; init; }
    public double P50Latency { get; init; }
    public double P95Latency { get; init; }
    public double P99Latency { get; init; }
    public int DailyVolume { get; init; }
    public double FailureRate { get; init; }
    public int ConsecutiveFailures { get; init; }

    public PerformanceMetrics(
        double successRate = 1.0,
        double p50Latency = 100,
        double p95Latency = 200,
        double p99Latency = 500,
        int dailyVolume = 0,
        double failureRate = 0.0,
        int consecutiveFailures = 0)
    {
        SuccessRate = successRate;
        P50Latency = p50Latency;
        P95Latency = p95Latency;
        P99Latency = p99Latency;
        DailyVolume = dailyVolume;
        FailureRate = failureRate;
        ConsecutiveFailures = consecutiveFailures;
    }

    public PerformanceMetrics WithSuccessRate(double successRate) =>
        this with { SuccessRate = successRate };

    public PerformanceMetrics WithConsecutiveFailures(int failures) =>
        this with { ConsecutiveFailures = failures };

    public PerformanceMetrics IncrementFailures() =>
        this with { ConsecutiveFailures = ConsecutiveFailures + 1 };

    public PerformanceMetrics ResetFailures() =>
        this with { ConsecutiveFailures = 0 };
}
