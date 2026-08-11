using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace PaymentProcessing.Features.Shared.Clients;

/// <summary>
/// HTTP client for Bank Adapter service
/// </summary>
public sealed class BankAdapterClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BankAdapterClient> _logger;

    public BankAdapterClient(
        HttpClient httpClient,
        ILogger<BankAdapterClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Authorize payment with provider synchronously
    /// </summary>
    public async Task<(bool Success, AuthorizeSuccessResponse? Result, string? Error)> AuthorizeAsync(AuthorizeRequest request, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.Source.StartActivity("BankAdapter.Authorize");
        activity?.SetTag("payment.id", request.PaymentId);
        activity?.SetTag("provider.id", request.ProviderId);

        try
        {
            _logger.LogInformation("Calling Bank Adapter for payment {PaymentId} with provider {ProviderId}",
                request.PaymentId, request.ProviderId);

            var response = await _httpClient.PostAsJsonAsync(
                "/api/bank/authorize",
                request,
                cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            // Handle both success and error responses
            if (response.IsSuccessStatusCode)
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<AuthorizeSuccessResponse>(responseContent);

                _logger.LogInformation("Bank authorization succeeded for payment {PaymentId}: TransactionId={TransactionId}",
                    request.PaymentId, result?.ProviderTransactionId);

                activity?.SetTag("bank.transaction_id", result?.ProviderTransactionId);
                activity?.SetTag("bank.amount", result?.Metadata?.Amount);

                return (true, result, null);
            }
            else
            {
                var errorResult = System.Text.Json.JsonSerializer.Deserialize<AuthorizeErrorResponse>(responseContent);

                _logger.LogWarning("Bank authorization failed for payment {PaymentId}: ErrorCode={ErrorCode}",
                    request.PaymentId, errorResult?.ErrorCode);

                activity?.SetTag("bank.error_code", errorResult?.ErrorCode);
                activity?.SetStatus(ActivityStatusCode.Error);

                return (false, null, $"Bank authorization failed: {errorResult?.ErrorCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Bank Adapter service unavailable for payment {PaymentId}", request.PaymentId);
            activity?.SetStatus(ActivityStatusCode.Error);
            return (false, null, "Bank Adapter service unavailable");
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Bank Adapter request timed out for payment {PaymentId}", request.PaymentId);
            activity?.SetStatus(ActivityStatusCode.Error);
            return (false, null, "Bank Adapter request timed out");
        }
    }
}

/// <summary>
/// Request models for Bank Adapter service
/// </summary>
public sealed record AuthorizeRequest
{
    public string PaymentId { get; init; }
    public string ProviderId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Response models from Bank Adapter service
/// </summary>
public sealed record AuthorizeSuccessResponse
{
    public string PaymentId { get; init; }
    public string ProviderId { get; init; }
    public string ProviderTransactionId { get; init; }
    public bool Success { get; init; }
    public BankMetadata? Metadata { get; init; }
}

public sealed record BankMetadata
{
    public string Status { get; init; }
    public string? ClientSecret { get; init; }
    public long? Amount { get; init; }
    public string Currency { get; init; }
}

public sealed record AuthorizeErrorResponse
{
    public string Error { get; init; }
    public string ErrorCode { get; init; }
    public string? Message { get; init; }
}