using System.Diagnostics.Metrics;

namespace Shared.Observability;

/// <summary>
/// OpenTelemetry Metrics
/// Exported through OTel Collector -> Prometheus
/// </summary>
public static class PaymentMetrics
{
    public const string MeterName = "PaymentSystem";

    public static readonly Meter Meter = new(MeterName, "1.0.0");

    // Counter: Total payments processed
    public static readonly Counter<long> PaymentsTotal =
        Meter.CreateCounter<long>(
            "payments_total",
            description: "Total payments processed");

    // UpDownCounter works better than Gauge for request tracking
    public static readonly UpDownCounter<long> ActivePayments =
        Meter.CreateUpDownCounter<long>(
            "active_payments",
            description: "Currently processing payments");

    // Histogram: Payment processing duration
    public static readonly Histogram<double> PaymentDuration =
        Meter.CreateHistogram<double>(
            "payment_duration_seconds",
            unit: "s",
            description: "Payment processing duration");

    // Counter: Provider-specific errors
    public static readonly Counter<long> ProviderErrors =
        Meter.CreateCounter<long>(
            "provider_errors_total",
            description: "Total provider errors");

    // Counter: Risk assessment rejections
    public static readonly Counter<long> RiskRejections =
        Meter.CreateCounter<long>(
            "risk_rejections_total",
            description: "Total risk assessment rejections");

    // Histogram: Provider latency
    public static readonly Histogram<double> ProviderLatency =
        Meter.CreateHistogram<double>(
            "provider_latency_seconds",
            unit: "s",
            description: "Provider response latency");

    // ObservableGauge: Event queue depth
    private static long _eventQueueDepth;

    public static readonly ObservableGauge<long> EventQueueDepth =
        Meter.CreateObservableGauge(
            "event_queue_depth",
            () => _eventQueueDepth,
            description: "Current event queue depth");

    // Counter: Database errors
    public static readonly Counter<long> DatabaseErrors =
        Meter.CreateCounter<long>(
            "database_errors_total",
            description: "Total database errors");

    // Histogram: Database latency
    public static readonly Histogram<double> DatabaseLatency =
        Meter.CreateHistogram<double>(
            "database_latency_seconds",
            unit: "s",
            description: "Database query latency");

    // Histogram: Bank latency
    public static readonly Histogram<double> BankLatency =
        Meter.CreateHistogram<double>(
            "bank_request_duration_seconds",
            unit: "s",
            description: "Bank request duration");

    // Counter: Routing decisions
    public static readonly Counter<long> RoutingDecisionsTotal =
        Meter.CreateCounter<long>(
            "routing_decisions_total",
            description: "Total routing decisions made");

    // Histogram: Routing latency
    public static readonly Histogram<double> RoutingDecisionLatency =
        Meter.CreateHistogram<double>(
            "routing_decision_duration_seconds",
            unit: "s",
            description: "Routing decision duration");

    public static void SetEventQueueDepth(long depth)
    {
        _eventQueueDepth = depth;
    }
}