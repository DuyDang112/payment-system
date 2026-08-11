using System.Diagnostics;
using Prometheus;
using Serilog;
using Serilog.Events;
using Serilog.Enrichers;
using Serilog.Formatting.Compact;

namespace Shared.Observability;

/// <summary>
/// Shared observability configuration for all payment services
/// Ensures consistent field names, metrics, and span attributes across the system
/// </summary>

// ===== PROMETHEUS METRICS WITH PROPER LABEL CARDINALITY =====
// Labels must be bounded categorical values - NEVER unbounded IDs
public static class PrometheusMetrics
{
    // Counter: Total payments processed
    // Labels: status (bounded), currency (bounded), provider (bounded), risk_decision (bounded)
    public static readonly Counter PaymentsTotal = Metrics
        .CreateCounter("payments_total", "Total payments processed",
            "status", "currency", "provider", "risk_decision");

    // Gauge: Currently active payments (in-flight requests)
    public static readonly Gauge ActivePayments = Metrics
        .CreateGauge("active_payments", "Currently processing payments");

    // Histogram: Payment processing duration with latency percentiles
    // Labels: status, currency, provider (all bounded)
    public static readonly Histogram PaymentDuration = Metrics
        .CreateHistogram("payment_duration_seconds", "Payment processing duration",
            "status", "currency", "provider");

    // Counter: Provider-specific errors
    // Labels: provider (bounded), error_type (bounded)
    public static readonly Counter ProviderErrors = Metrics
        .CreateCounter("provider_errors_total", "Total provider errors",
            "provider", "error_type");

    // Counter: Risk assessment rejections
    // Labels: reason (bounded - high_amount, suspicious_location, blacklist, etc.)
    public static readonly Counter RiskRejections = Metrics
        .CreateCounter("risk_rejections_total", "Total risk assessment rejections",
            "reason");

    // Histogram: Provider response times for latency percentiles (P50, P95, P99)
    // Labels: provider (bounded), operation (bounded - authorize, capture, refund, void)
    public static readonly Histogram ProviderLatency = Metrics
        .CreateHistogram("provider_latency_seconds", "Provider response latency",
            "provider", "operation");

    // Gauge: Event queue depth (detects when consumers stop processing)
    public static readonly Gauge EventQueueDepth = Metrics
        .CreateGauge("event_queue_depth", "Current event queue depth");

    // Counter: Database operation failures
    // Labels: operation (bounded), table (bounded)
    public static readonly Counter DatabaseErrors = Metrics
        .CreateCounter("database_errors_total", "Total database errors",
            "operation", "table");

    // Histogram: Database query duration
    // Labels: operation (bounded), table (bounded)
    public static readonly Histogram DatabaseLatency = Metrics
        .CreateHistogram("database_latency_seconds", "Database query latency",
            "operation", "table");

    // Histogram: Bank request duration
    // Labels: bank (bounded), operation (bounded)
    public static readonly Histogram BankLatency = Metrics
        .CreateHistogram("bank_request_duration_ms", "Bank request duration",
            "bank", "operation");

    // Counter: Routing decisions made
    // Labels: strategy (bounded), decision_reason (bounded), eligible_provider_count_category (bounded)
    public static readonly Counter RoutingDecisionsTotal = Metrics
        .CreateCounter("routing_decisions_total", "Total routing decisions made",
            "strategy", "decision_reason", "eligible_providers");

    // Histogram: Routing decision latency
    // Labels: strategy (bounded), decision_reason (bounded)
    public static readonly Histogram RoutingDecisionLatency = Metrics
        .CreateHistogram("routing_decision_duration_seconds", "Routing decision duration",
            "strategy", "decision_reason");

}


