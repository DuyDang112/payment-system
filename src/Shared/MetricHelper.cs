using System;
using Shared.Observability;

namespace Shared;

/// <summary>
/// Helper methods for consistent metric tracking across services
/// </summary>
public static class MetricHelper
{
    /// <summary>
    /// Record payment completion with proper bounded labels
    /// </summary>
    public static void RecordPaymentCompletion(
        string status,
        string currency,
        string provider,
        string riskDecision,
        double durationSeconds)
    {
        PrometheusMetrics.PaymentsTotal
            .WithLabels(status, currency, provider, riskDecision)
            .Inc();

        PrometheusMetrics.PaymentDuration
            .WithLabels(status, currency, provider)
            .Observe(durationSeconds);
    }

    /// <summary>
    /// Record provider error with proper bounded labels
    /// </summary>
    public static void RecordProviderError(string provider, string errorType)
    {
        PrometheusMetrics.ProviderErrors
            .WithLabels(provider, errorType)
            .Inc();
    }

    /// <summary>
    /// Record risk rejection with proper bounded labels
    /// </summary>
    public static void RecordRiskRejection(string reason)
    {
        PrometheusMetrics.RiskRejections
            .WithLabels(reason)
            .Inc();
    }

    /// <summary>
    /// Record provider latency for P50/P95/P99 analysis
    /// </summary>
    public static void RecordProviderLatency(string provider, string operation, double durationSeconds)
    {
        PrometheusMetrics.ProviderLatency
            .WithLabels(provider, operation)
            .Observe(durationSeconds);
    }

    /// <summary>
    /// Record bank latency for P50/P95/P99 analysis
    /// </summary>
    public static void RecordBankLatency(string bank, string operation, double durationSeconds)
    {
        PrometheusMetrics.BankLatency
            .WithLabels(bank, operation)
            .Observe(durationSeconds);
    }

    /// <summary>
    /// Record routing decision with readable labels
    /// </summary>
    public static void RecordRoutingDecision(
        string strategy,
        string decisionReason,
        int eligibleProviderCount,
        double durationSeconds)
    {
        // Convert provider count to bounded category
        var eligibleProvidersCategory = eligibleProviderCount switch
        {
            0 => "none",
            1 => "one",
            <= 3 => "few",
            _ => "many"
        };

        PrometheusMetrics.RoutingDecisionsTotal
            .WithLabels(strategy, decisionReason, eligibleProvidersCategory)
            .Inc();

        PrometheusMetrics.RoutingDecisionLatency
            .WithLabels(strategy, decisionReason)
            .Observe(durationSeconds);
    }
}
