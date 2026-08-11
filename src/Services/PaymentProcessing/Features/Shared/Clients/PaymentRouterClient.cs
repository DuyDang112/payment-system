using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace PaymentProcessing.Features.Shared.Clients;

/// <summary>
/// HTTP client for Payment Router service
/// </summary>
public sealed class PaymentRouterClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentRouterClient> _logger;

    public PaymentRouterClient(
        HttpClient httpClient,
        ILogger<PaymentRouterClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Route payment to best provider synchronously
    /// </summary>
    public async Task<(bool Success, RoutePaymentResponse? Result, string? Error)> RoutePaymentAsync(RoutePaymentRequest request, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.Source.StartActivity("PaymentRouter.RoutePayment");
        activity?.SetTag("payment.id", request.PaymentId);
        activity?.SetTag("merchant.id", request.MerchantId);

        try
        {
            _logger.LogInformation("Calling Payment Router for payment {PaymentId}", request.PaymentId);

            var response = await _httpClient.PostAsJsonAsync(
                "/api/routing/route",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<RoutePaymentResponse>(cancellationToken);

            _logger.LogInformation("Payment routed for {PaymentId}: Provider={Provider}, Cost={Cost}",
                request.PaymentId, result?.SelectedProviderId, result?.CostEstimate);

            activity?.SetTag("router.selected_provider", result?.SelectedProviderId);
            activity?.SetTag("router.strategy", result?.Strategy);

            if (result != null)
            {
                return (true, result, null);
            }
            else
            {
                return (false, null, "Unable to route payment");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Payment Router service unavailable for payment {PaymentId}", request.PaymentId);
            activity?.SetStatus(ActivityStatusCode.Error);
            return (false, null, "Payment Router service unavailable");
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Payment Router request timed out for payment {PaymentId}", request.PaymentId);
            activity?.SetStatus(ActivityStatusCode.Error);
            return (false, null, "Payment Router request timed out");
        }
    }
}