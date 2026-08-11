using System.Diagnostics;

namespace PaymentProcessing.Features.Shared;

/// <summary>
/// OpenTelemetry diagnostics configuration for distributed tracing
/// </summary>
public static class DiagnosticsConfig
{
    public static ActivitySource Source = new ActivitySource("PaymentProcessing");
}
