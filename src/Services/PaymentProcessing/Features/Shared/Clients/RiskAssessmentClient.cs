using Microsoft.AspNetCore.Mvc;
using PaymentProcessing.Features.CreatePayment;
using System.Diagnostics;

namespace PaymentProcessing.Features.Shared.Clients;

/// <summary>
/// HTTP client for Risk Assessment service
/// </summary>
public sealed class RiskAssessmentClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RiskAssessmentClient> _logger;

    public RiskAssessmentClient(
        HttpClient httpClient,
        ILogger<RiskAssessmentClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Evaluate payment risk synchronously
    /// </summary>
    public async Task<(bool Success, EvaluateRiskResponse? Result, string? Error)> EvaluateRiskAsync(EvaluateRiskRequest request, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.Source.StartActivity("RiskAssessment.EvaluateRisk");
        activity?.SetTag("payment.id", request.PaymentId);
        activity?.SetTag("merchant.id", request.MerchantId);

        try
        {
            _logger.LogInformation("Calling Risk Assessment for payment {PaymentId}", request.PaymentId);

            var response = await _httpClient.PostAsJsonAsync(
                "/api/risk/evaluate",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EvaluateRiskResponse>(cancellationToken);

            _logger.LogInformation("Risk Assessment completed for payment {PaymentId}: Score={Score}, Decision={Decision}",
                request.PaymentId, result?.RiskScore, result?.Decision);

            activity?.SetTag("risk.decision", result?.Decision);
            activity?.SetTag("risk.score", result?.RiskScore);

            if (result?.Decision == "APPROVE")
            {
                return (true, result, null);
            }
            else
            {
                return (false, null, $"Payment rejected by risk assessment: {result?.Decision}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Risk Assessment service unavailable for payment {PaymentId}", request.PaymentId);
            activity?.SetStatus(ActivityStatusCode.Error);
            return (false, null, "Risk Assessment service unavailable");
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Risk Assessment request timed out for payment {PaymentId}", request.PaymentId);
            activity?.SetStatus(ActivityStatusCode.Error);
            return (false, null, "Risk Assessment request timed out");
        }
    }
}