using System;
using Shared.Observability;

namespace Shared;

/// <summary>
/// Helper methods for consistent metric tracking across services
/// </summary>

public static class MetricHelper
{
    /// <summary>
    /// Record payment completion with proper bounded tags
    /// </summary>
    public static void RecordPaymentCompletion(
        string status,
        string currency,
        string provider,
        string riskDecision,
        double durationSeconds)
    {
        PaymentMetrics.PaymentsTotal.Add(
            1,
            new KeyValuePair<string, object?>("status", status),
            new KeyValuePair<string, object?>("currency", currency),
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("risk_decision", riskDecision));

        PaymentMetrics.PaymentDuration.Record(
            durationSeconds,
            new KeyValuePair<string, object?>("status", status),
            new KeyValuePair<string, object?>("currency", currency),
            new KeyValuePair<string, object?>("provider", provider));
    }

    /// <summary>
    /// Record provider error with proper bounded tags
    /// </summary>
    public static void RecordProviderError(
        string provider,
        string errorType)
    {
        PaymentMetrics.ProviderErrors.Add(
            1,
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("error_type", errorType));
    }

    /// <summary>
    /// Record risk rejection with proper bounded tags
    /// </summary>
    public static void RecordRiskRejection(string reason)
    {
        PaymentMetrics.RiskRejections.Add(
            1,
            new KeyValuePair<string, object?>("reason", reason));
    }

    /// <summary>
    /// Record provider latency for P50/P95/P99 analysis
    /// </summary>
    public static void RecordProviderLatency(
        string provider,
        string operation,
        double durationSeconds)
    {
        PaymentMetrics.ProviderLatency.Record(
            durationSeconds,
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("operation", operation));
    }

    /// <summary>
    /// Record bank latency for P50/P95/P99 analysis
    /// </summary>
    public static void RecordBankLatency(
        string bank,
        string operation,
        double durationSeconds)
    {
        PaymentMetrics.BankLatency.Record(
            durationSeconds,
            new KeyValuePair<string, object?>("bank", bank),
            new KeyValuePair<string, object?>("operation", operation));
    }

    /// <summary>
    /// Record routing decision with readable tags
    /// </summary>
    public static void RecordRoutingDecision(
        string strategy,
        string decisionReason,
        int eligibleProviderCount,
        double durationSeconds)
    {
        var eligibleProvidersCategory = eligibleProviderCount switch
        {
            0 => "none",
            1 => "one",
            <= 3 => "few",
            _ => "many"
        };

        PaymentMetrics.RoutingDecisionsTotal.Add(
            1,
            new KeyValuePair<string, object?>("strategy", strategy),
            new KeyValuePair<string, object?>("decision_reason", decisionReason),
            new KeyValuePair<string, object?>("eligible_providers", eligibleProvidersCategory));

        PaymentMetrics.RoutingDecisionLatency.Record(
            durationSeconds,
            new KeyValuePair<string, object?>("strategy", strategy),
            new KeyValuePair<string, object?>("decision_reason", decisionReason));
    }

    /// <summary>
    /// Update current queue depth
    /// </summary>
    public static void SetQueueDepth(long depth)
    {
        PaymentMetrics.SetEventQueueDepth(depth);
    }

    /// <summary>
    /// Track active payments
    /// </summary>
    public static void IncrementActivePayments()
    {
        PaymentMetrics.ActivePayments.Add(1);
    }

    /// <summary>
    /// Track active payments
    /// </summary>
    public static void DecrementActivePayments()
    {
        PaymentMetrics.ActivePayments.Add(-1);
    }

    /// <summary>
    /// Record database errors
    /// </summary>
    public static void RecordDatabaseError(
        string operation,
        string table)
    {
        PaymentMetrics.DatabaseErrors.Add(
            1,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("table", table));
    }

    /// <summary>
    /// Record database latency
    /// </summary>
    public static void RecordDatabaseLatency(
        string operation,
        string table,
        double durationSeconds)
    {
        PaymentMetrics.DatabaseLatency.Record(
            durationSeconds,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("table", table));
    }
}
