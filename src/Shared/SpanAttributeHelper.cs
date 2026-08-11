using System.Diagnostics;

namespace Shared;

/// <summary>
/// Safe span attribute enrichment helpers
/// </summary>
public static class SpanAttributeHelper
{
    /// <summary>
    /// Add business attributes to span (NEVER sensitive data)
    /// </summary>
    public static void AddPaymentAttributes(
        this Activity activity,
        string currency,
        string? status = null,
        string? provider = null,
        string? riskDecision = null)
    {
        activity.SetTag(SpanAttributes.Currency, currency);

        if (!string.IsNullOrEmpty(status))
            activity.SetTag(SpanAttributes.Status, status);

        if (!string.IsNullOrEmpty(provider))
            activity.SetTag(SpanAttributes.Provider, provider);

        if (!string.IsNullOrEmpty(riskDecision))
            activity.SetTag(SpanAttributes.RiskDecision, riskDecision);
    }

    /// <summary>
    /// Add HTTP attributes to span
    /// </summary>
    public static void AddHttpAttributes(
        this Activity activity,
        string method,
        string route,
        int statusCode)
    {
        activity.SetTag(SpanAttributes.HttpMethod, method);
        activity.SetTag(SpanAttributes.HttpRoute, route);
        activity.SetTag(SpanAttributes.HttpStatusCode, statusCode);
    }

    /// <summary>
    /// Add error attributes to span (no sensitive data in messages)
    /// </summary>
    public static void AddErrorAttributes(
        this Activity activity,
        string errorType)
    {
        activity.SetTag(SpanAttributes.ErrorType, errorType);
        // NEVER include exception messages that might contain sensitive data
    }
}

// ===== SAFE SPAN ATTRIBUTES =====
// NEVER include sensitive data: card numbers, CVV, account numbers, auth tokens, raw payloads
public static class SpanAttributes
{
    // Service attributes
    public const string ServiceName = "service.name";
    public const string ServiceNamespace = "service.namespace";
    public const string ServiceVersion = "service.version";

    // HTTP attributes
    public const string HttpMethod = "http.method";
    public const string HttpUrl = "http.url";
    public const string HttpStatusCode = "http.status_code";
    public const string HttpRoute = "http.route";

    // Business attributes (bounded, categorical values only)
    public const string Currency = "payment.currency";
    public const string Status = "payment.status";
    public const string Provider = "payment.provider";
    public const string RiskDecision = "risk.decision";
    public const string ErrorType = "error.type";
    public const string Operation = "operation.name";

    // Database attributes
    public const string DbSystem = "db.system";
    public const string DbName = "db.name";
    public const string DbOperation = "db.operation";
    public const string DbTable = "db.table";
}
