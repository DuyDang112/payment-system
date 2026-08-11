using BankAdapter.Domain;
using BankAdapter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BankAdapter.Infrastructure.Logging;

/// <summary>
/// Request logging service with PCI-DSS sanitization
/// </summary>
public sealed class RequestLoggingService(
    BankAdapterDbContext context,
    ILogger<RequestLoggingService> logger)
{
    /// <summary>
    /// Create a new request log entry
    /// </summary>
    public async Task<ProviderRequestLog> CreateLogAsync(
        string paymentId,
        string providerId,
        Operation operation,
        object requestPayload,
        CancellationToken cancellationToken = default)
    {
        var sanitizedRequest = SanitizePayload(JsonSerializer.Serialize(requestPayload));

        var log = ProviderRequestLog.Create(
            paymentId,
            providerId,
            operation,
            sanitizedRequest);

        await context.ProviderRequestLogs.AddAsync(log, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created request log {LogId} for payment {PaymentId} to provider {ProviderId}",
            log.LogId,
            paymentId,
            providerId);

        return log;
    }

    /// <summary>
    /// Update log with successful response
    /// </summary>
    public async Task UpdateLogSuccessAsync(
        ProviderRequestLog log,
        object responsePayload,
        int httpStatusCode,
        CancellationToken cancellationToken = default)
    {
        var sanitizedResponse = SanitizePayload(JsonSerializer.Serialize(responsePayload));

        log.CompleteSuccess(sanitizedResponse, httpStatusCode);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Request log {LogId} completed successfully in {Duration}ms",
            log.LogId,
            log.Duration);
    }

    /// <summary>
    /// Update log with error response
    /// </summary>
    public async Task UpdateLogErrorAsync(
        ProviderRequestLog log,
        object responsePayload,
        int httpStatusCode,
        string? errorCode = null,
        string? errorMessage = null,
        ProviderResult result = ProviderResult.FatalError,
        CancellationToken cancellationToken = default)
    {
        var sanitizedResponse = SanitizePayload(JsonSerializer.Serialize(responsePayload));

        log.CompleteError(sanitizedResponse, httpStatusCode, errorCode, errorMessage, result);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Request log {LogId} completed with error: {ErrorCode} - {ErrorMessage}",
            log.LogId,
            errorCode,
            errorMessage);
    }

    /// <summary>
    /// Increment retry count for log entry
    /// </summary>
    public async Task IncrementRetryAsync(
        ProviderRequestLog log,
        CancellationToken cancellationToken = default)
    {
        log.IncrementRetry();

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Request log {LogId} retry count incremented to {RetryCount}",
            log.LogId,
            log.RetryCount);
    }

    /// <summary>
    /// Sanitize payload to remove sensitive data (PCI-DSS compliance)
    /// </summary>
    private string SanitizePayload(string payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            return string.Empty;
        }

        var sanitized = payload;

        // Remove card numbers (PCI-DSS requirement)
        sanitized = Regex.Replace(
            sanitized,
            @"\""cardNumber\""\s*:\s*\""?\d[0-9]{11,}\""?",
            @"""cardNumber"":""XXX-XXX-XXX-XXX""",
            RegexOptions.IgnoreCase);

        sanitized = Regex.Replace(
            sanitized,
            @"\""number\""\s*:\s*\""?\d[0-9]{11,}\""?",
            @"""number"":""XXX-XXX-XXX-XXX""",
            RegexOptions.IgnoreCase);

        // Remove CVV/CVC codes
        sanitized = Regex.Replace(
            sanitized,
            @"\""cvc\""\s*:\s*\""\d{3,4}\""",
            @"""cvc"":""XXX""",
            RegexOptions.IgnoreCase);

        sanitized = Regex.Replace(
            sanitized,
            @"\""cvv\""\s*:\s*\""\d{3,4}\""",
            @"""cvv"":""XXX""",
            RegexOptions.IgnoreCase);

        // Remove PINs
        sanitized = Regex.Replace(
            sanitized,
            @"\""pin\""\s*:\s*\""\d+\""",
            @"""pin"":""XXX""",
            RegexOptions.IgnoreCase);

        // Remove account numbers
        sanitized = Regex.Replace(
            sanitized,
            @"\""accountNumber\""\s*:\s*\""\d+\""",
            @"""accountNumber"":""XXX-XXX""",
            RegexOptions.IgnoreCase);

        // Remove routing numbers
        sanitized = Regex.Replace(
            sanitized,
            @"\""routingNumber\""\s*:\s*\""\d+\""",
            @"""routingNumber"":""XXX""",
            RegexOptions.IgnoreCase);

        // Remove API keys and tokens
        sanitized = Regex.Replace(
            sanitized,
            @"\""api[_-]?key\""\s*:\s*\""[^\""]+\""",
            @"""api_key"":""XXX""",
            RegexOptions.IgnoreCase);

        sanitized = Regex.Replace(
            sanitized,
            @"""(Bearer|Basic)\s+[A-Za-z0-9\-._~+/]+=*""",
            @"""Bearer XXX""",
            RegexOptions.IgnoreCase);

        // Remove passwords
        sanitized = Regex.Replace(
            sanitized,
            @"\""password\""\s*:\s*\""[^\""]+\""",
            @"""password"":""XXX""",
            RegexOptions.IgnoreCase);

        // Remove security codes
        sanitized = Regex.Replace(
            sanitized,
            @"\""securityCode\""\s*:\s*\""\d+\""",
            @"""securityCode"":""XXX""",
            RegexOptions.IgnoreCase);

        // Remove PAN data (Primary Account Number)
        sanitized = Regex.Replace(
            sanitized,
            @"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b",
            "XXXXXXXXXXXXXXX");

        // Remove expiration dates (keep for now, may be needed for debugging)
        // If needed to sanitize:
        // sanitized = Regex.Replace(sanitized, @"""expiryDate""\s*:\s*""\d{2}/\d{4}""", @"""expiryDate"":""XX/XXXX""", RegexOptions.IgnoreCase);

        return sanitized;
    }

    /// <summary>
    /// Get request history for payment
    /// </summary>
    public async Task<List<ProviderRequestLog>> GetRequestHistoryAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        return await context.ProviderRequestLogs
            .Where(log => log.PaymentId == paymentId)
            .OrderByDescending(log => log.RequestSentAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get request logs for provider within time range
    /// </summary>
    public async Task<List<ProviderRequestLog>> GetProviderLogsAsync(
        string providerId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        return await context.ProviderRequestLogs
            .Where(log => log.ProviderId == providerId &&
                         log.RequestSentAt >= startTime &&
                         log.RequestSentAt <= endTime)
            .OrderByDescending(log => log.RequestSentAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get error logs for monitoring
    /// </summary>
    public async Task<List<ProviderRequestLog>> GetErrorLogsAsync(
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        return await context.ProviderRequestLogs
            .Where(log => log.RequestSentAt >= since &&
                         log.Result == ProviderResult.FatalError)
            .OrderByDescending(log => log.RequestSentAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }
}
