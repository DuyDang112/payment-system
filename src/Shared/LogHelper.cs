using System;

namespace Shared;

/// <summary>
/// Helper methods for consistent structured logging across services
/// </summary>
public static class LogHelper
{
    /// <summary>
    /// Create consistent payment log context
    /// </summary>
    public static IDictionary<string, object> CreatePaymentContext(
        string paymentId,
        string merchantId,
        string customerId,
        string currency,
        decimal amount,
        string? provider = null,
        string? status = null,
        string? riskDecision = null)
    {
        var context = new Dictionary<string, object>
        {
            [LogFields.PaymentId] = paymentId,
            [LogFields.MerchantId] = merchantId,
            [LogFields.CustomerId] = customerId,
            [LogFields.Currency] = currency,
            [LogFields.Amount] = amount
        };

        if (!string.IsNullOrEmpty(provider))
            context[LogFields.Provider] = provider;

        if (!string.IsNullOrEmpty(status))
            context[LogFields.Status] = status;

        if (!string.IsNullOrEmpty(riskDecision))
            context[LogFields.RiskDecision] = riskDecision;

        return context;
    }

    /// <summary>
    /// Create consistent error log context
    /// </summary>
    public static IDictionary<string, object> CreateErrorContext(
        string errorType,
        string errorCode,
        string errorMessage,
        IDictionary<string, string>? additionalContext = null)
    {
        var context = new Dictionary<string, object>
        {
            [LogFields.ErrorType] = errorType,
            [LogFields.ErrorCode] = errorCode,
            [LogFields.Error] = errorMessage
        };

        if (additionalContext != null)
        {
            foreach (var kvp in additionalContext)
            {
                context[kvp.Key] = kvp.Value;
            }
        }

        return context;
    }
}

// ===== CONSISTENT LOG FIELD NAMES =====
// All services use these exact field names for structured logging
public static class LogFields
{
    // Service identification
    public const string Service = "service";
    public const string ServiceVersion = "service_version";
    public const string Environment = "environment";

    // Observability context
    public const string TraceId = "trace_id";
    public const string SpanId = "span_id";
    public const string ParentSpanId = "parent_span_id";

    // Business context
    public const string PaymentId = "payment_id";
    public const string MerchantId = "merchant_id";
    public const string CustomerId = "customer_id";
    public const string Provider = "provider";
    public const string Currency = "currency";
    public const string Amount = "amount";
    public const string Status = "status";
    public const string RiskDecision = "risk_decision";
    public const string RiskScore = "risk_score";
    public const string Error = "error";
    public const string ErrorCode = "error_code";
    public const string ErrorType = "error_type";

    // HTTP context
    public const string HttpMethod = "http_method";
    public const string HttpPath = "http_path";
    public const string HttpStatusCode = "http_status_code";
    public const string Duration = "duration_ms";

    // Database context
    public const string DbOperation = "db_operation";
    public const string DbTable = "db_table";
    public const string DbRowsAffected = "rows_affected";
}
