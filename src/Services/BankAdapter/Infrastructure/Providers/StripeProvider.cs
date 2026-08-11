using BankAdapter.Domain;
using BankAdapter.Features.Shared.Errors;
using BankAdapter.Shared;
using System.Text.Json;

namespace BankAdapter.Infrastructure.Providers;

/// <summary>
/// Stripe provider implementation
/// </summary>
public sealed partial class StripeProvider(
    Provider providerConfig,
    HttpClient httpClient,
    ILogger<StripeProvider> logger) : IProvider
{
    public string ProviderId => providerConfig.ProviderId;
    public ProviderType ProviderType => ProviderType.Stripe;

    public async Task<Result<ProviderResponse>> AuthorizeAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Stripe authorize request for payment {PaymentId}", request.PaymentId);

        try
        {
            var stripeRequest = new
            {
                amount = (long)(request.Amount * 100), // Convert to cents
                currency = request.Currency.ToLower(),
                payment_method = request.Metadata.GetValueOrDefault("payment_method_id"),
                capture_method = "manual",
                metadata = new Dictionary<string, string>
                {
                    { "payment_id", request.PaymentId },
                    { "operation", "authorize" }
                }
            };

            var jsonRequest = JsonSerializer.Serialize(stripeRequest);
            var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(
                $"{providerConfig.Endpoint}/v1/payment_intents",
                content,
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            if (!response.IsSuccessStatusCode)
            {
                var error = jsonResponse.GetProperty("error");
                var errorCode = error.GetProperty("code").GetString();
                var errorMessage = error.GetProperty("message").GetString();

                logger.LogWarning("Stripe authorize failed: {ErrorCode} - {ErrorMessage}", errorCode, errorMessage);

                return Result<ProviderResponse>.Failure(ProviderErrors.ProviderError(
                    ProviderId,
                    errorCode ?? "unknown",
                    errorMessage ?? "Authorization failed"
                ));
            }

            var transactionId = jsonResponse.GetProperty("id").GetString();
            var status = jsonResponse.GetProperty("status").GetString();

            logger.LogInformation("Stripe authorize succeeded: {TransactionId}", transactionId);

            return Result<ProviderResponse>.Success(new ProviderResponse(
                transactionId ?? string.Empty,
                ProviderId,
                status == "requires_capture",
                null,
                null,
                new Dictionary<string, string>
                {
                    { "status", status ?? "unknown" },
                    { "client_secret", jsonResponse.GetProperty("client_secret").GetString() ?? string.Empty }
                }
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stripe authorize request failed for payment {PaymentId}", request.PaymentId);
            return Result<ProviderResponse>.Failure(ProviderErrors.ProviderError(ProviderId, "exception", ex.Message));
        }
    }

    public async Task<Result<ProviderResponse>> CaptureAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Stripe capture request for payment {PaymentId}", request.PaymentId);

        if (string.IsNullOrEmpty(request.AuthorizationToken))
        {
            return Result<ProviderResponse>.Failure(ProviderErrors.MissingAuthorizationToken);
        }

        try
        {
            var stripeRequest = new
            {
                amount = (long)(request.Amount * 100) // Convert to cents
            };

            var jsonRequest = JsonSerializer.Serialize(stripeRequest);
            var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(
                $"{providerConfig.Endpoint}/v1/payment_intents/{request.AuthorizationToken}/capture",
                content,
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            if (!response.IsSuccessStatusCode)
            {
                var error = jsonResponse.GetProperty("error");
                var errorCode = error.GetProperty("code").GetString();
                var errorMessage = error.GetProperty("message").GetString();

                logger.LogWarning("Stripe capture failed: {ErrorCode} - {ErrorMessage}", errorCode, errorMessage);

                return Result<ProviderResponse>.Failure(ProviderErrors.ProviderError(
                    ProviderId,
                    errorCode ?? "unknown",
                    errorMessage ?? "Capture failed"
                ));
            }

            var transactionId = jsonResponse.GetProperty("id").GetString();
            var status = jsonResponse.GetProperty("status").GetString();

            logger.LogInformation("Stripe capture succeeded: {TransactionId}", transactionId);

            return Result<ProviderResponse>.Success(new ProviderResponse(
                transactionId ?? string.Empty,
                ProviderId,
                status == "succeeded",
                null,
                null,
                new Dictionary<string, string>
                {
                    { "status", status ?? "unknown" }
                }
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stripe capture request failed for payment {PaymentId}", request.PaymentId);
            return Result<ProviderResponse>.Failure(ProviderErrors.ProviderError(ProviderId, "exception", ex.Message));
        }
    }

    public async Task<Result<ProviderResponse>> RefundAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Stripe refund request for payment {PaymentId}", request.PaymentId);

        if (string.IsNullOrEmpty(request.AuthorizationToken))
        {
            return Result<ProviderResponse>.Failure(ProviderErrors.MissingAuthorizationToken);
        }

        try
        {
            var stripeRequest = new
            {
                amount = (long)(request.Amount * 100), // Convert to cents
                reason = request.Metadata.GetValueOrDefault("reason", "requested_by_customer")
            };

            var jsonRequest = JsonSerializer.Serialize(stripeRequest);
            var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(
                $"{providerConfig.Endpoint}/v1/refunds",
                content,
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            if (!response.IsSuccessStatusCode)
            {
                var error = jsonResponse.GetProperty("error");
                var errorCode = error.GetProperty("code").GetString();
                var errorMessage = error.GetProperty("message").GetString();

                logger.LogWarning("Stripe refund failed: {ErrorCode} - {ErrorMessage}", errorCode, errorMessage);

                return Result<ProviderResponse>.Failure(ProviderErrors.ProviderError(
                    ProviderId,
                    errorCode ?? "unknown",
                    errorMessage ?? "Refund failed"
                ));
            }

            var transactionId = jsonResponse.GetProperty("id").GetString();
            var status = jsonResponse.GetProperty("status").GetString();

            logger.LogInformation("Stripe refund succeeded: {TransactionId}", transactionId);

            return Result<ProviderResponse>.Success(new ProviderResponse(
                transactionId ?? string.Empty,
                ProviderId,
                status == "succeeded",
                null,
                null,
                new Dictionary<string, string>
                {
                    { "status", status ?? "unknown" }
                }
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stripe refund request failed for payment {PaymentId}", request.PaymentId);
            return Result<ProviderResponse>.Failure(ProviderErrors.ProviderError(ProviderId, "exception", ex.Message));
        }
    }

    public async Task<Result<ProviderResponse>> VoidAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Stripe void request for payment {PaymentId}", request.PaymentId);

        if (string.IsNullOrEmpty(request.AuthorizationToken))
        {
            return Result<ProviderResponse>.Failure(ProviderErrors.MissingAuthorizationToken);
        }

        try
        {
            var response = await httpClient.PostAsync(
                $"{providerConfig.Endpoint}/v1/payment_intents/{request.AuthorizationToken}/cancel",
                null,
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            if (!response.IsSuccessStatusCode)
            {
                var error = jsonResponse.GetProperty("error");
                var errorCode = error.GetProperty("code").GetString();
                var errorMessage = error.GetProperty("message").GetString();

                logger.LogWarning("Stripe void failed: {ErrorCode} - {ErrorMessage}", errorCode, errorMessage);

                return Result<ProviderResponse>.Failure(ProviderErrors.ProviderError(
                    ProviderId,
                    errorCode ?? "unknown",
                    errorMessage ?? "Void failed"
                ));
            }

            var transactionId = jsonResponse.GetProperty("id").GetString();
            var status = jsonResponse.GetProperty("status").GetString();

            logger.LogInformation("Stripe void succeeded: {TransactionId}", transactionId);

            return Result<ProviderResponse>.Success(new ProviderResponse(
                transactionId ?? string.Empty,
                ProviderId,
                status == "canceled",
                null,
                null,
                new Dictionary<string, string>
                {
                    { "status", status ?? "unknown" }
                }
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stripe void request failed for payment {PaymentId}", request.PaymentId);
            return Result<ProviderResponse>.Failure(ProviderErrors.ProviderError(ProviderId, "exception", ex.Message));
        }
    }

    public async Task<Result<ProviderHealthStatus>> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Checking Stripe provider health");

            var response = await httpClient.GetAsync(
                $"{providerConfig.Endpoint}/v1/products",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Result<ProviderHealthStatus>.Success(new ProviderHealthStatus(
                    true,
                    "Stripe API is reachable"
                ));
            }

            return Result<ProviderHealthStatus>.Success(new ProviderHealthStatus(
                false,
                $"Stripe API returned {response.StatusCode}"
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stripe health check failed");
            return Result<ProviderHealthStatus>.Success(new ProviderHealthStatus(
                false,
                ex.Message
            ));
        }
    }

    public bool SupportsOperation(Operation operation)
    {
        return providerConfig.SupportsOperation(operation);
    }
}
