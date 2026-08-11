using BankAdapter.Domain;
using BankAdapter.Features.Shared.Errors;
using BankAdapter.Infrastructure.Data;
using BankAdapter.Shared;
using Microsoft.EntityFrameworkCore;

namespace BankAdapter.Infrastructure.Providers;

/// <summary>
/// Factory for creating provider instances
/// </summary>
public sealed class ProviderFactory(
    BankAdapterDbContext context,
    IHttpClientFactory httpClientFactory,
    ILogger<ProviderFactory> logger)
{
    /// <summary>
    /// Get provider instance by provider ID
    /// </summary>
    public async Task<Result<IProvider>> GetProviderAsync(
        string providerId,
        CancellationToken cancellationToken = default)
    {
        var providerConfig = await context.Providers
            .FirstOrDefaultAsync(p => p.ProviderId == providerId && p.IsActive, cancellationToken);

        if (providerConfig == null)
        {
            logger.LogWarning("Provider {ProviderId} not found or inactive", providerId);
            return Result<IProvider>.Failure(ProviderErrors.ProviderNotFound);
        }

        var provider = CreateProvider(providerConfig);
        return Result<IProvider>.Success(provider);
    }

    /// <summary>
    /// Get provider instance by integration ID
    /// </summary>
    public async Task<Result<IProvider>> GetProviderByIntegrationIdAsync(
        string integrationId,
        CancellationToken cancellationToken = default)
    {
        var providerConfig = await context.Providers
            .FirstOrDefaultAsync(p => p.IntegrationId == integrationId && p.IsActive, cancellationToken);

        if (providerConfig == null)
        {
            logger.LogWarning("Provider with integration ID {IntegrationId} not found or inactive", integrationId);
            return Result<IProvider>.Failure(ProviderErrors.ProviderNotFound);
        }

        var provider = CreateProvider(providerConfig);
        return Result<IProvider>.Success(provider);
    }

    /// <summary>
    /// Create provider instance from configuration
    /// </summary>
    private IProvider CreateProvider(Provider providerConfig)
    {
        var httpClient = CreateHttpClient(providerConfig);

        return providerConfig.ProviderType switch
        {
            ProviderType.Stripe => new StripeProvider(providerConfig, httpClient,
                loggerFactory.CreateLogger<StripeProvider>()),
            ProviderType.Adyen => new AdyenProvider(providerConfig, httpClient,
                loggerFactory.CreateLogger<AdyenProvider>()),
            ProviderType.Bank => new BankProvider(providerConfig, httpClient,
                loggerFactory.CreateLogger<BankProvider>()),
            ProviderType.Wallet => new WalletProvider(providerConfig, httpClient,
                loggerFactory.CreateLogger<WalletProvider>()),
            _ => throw new NotSupportedException($"Provider type {providerConfig.ProviderType} is not supported")
        };
    }

    /// <summary>
    /// Create HTTP client for provider
    /// </summary>
    private HttpClient CreateHttpClient(Provider providerConfig)
    {
        var httpClient = httpClientFactory.CreateClient();

        httpClient.BaseAddress = new Uri(providerConfig.Endpoint);
        httpClient.Timeout = TimeSpan.FromMilliseconds(providerConfig.Timeouts.ReadTimeoutMs);

        // Add authentication headers
        if (providerConfig.AuthConfig.Type == AuthType.ApiKey)
        {
            var apiKey = providerConfig.AuthConfig.Credentials.GetValueOrDefault("api_key");
            if (!string.IsNullOrEmpty(apiKey))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            }
        }

        // Add common headers
        httpClient.DefaultRequestHeaders.Add("User-Agent", "BankAdapterService/1.0");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        return httpClient;
    }

    private readonly ILoggerFactory loggerFactory = new LoggerFactory();
}

/// <summary>
/// Adyen provider implementation
/// </summary>
public sealed class AdyenProvider(
    Provider providerConfig,
    HttpClient httpClient,
    ILogger<AdyenProvider> logger) : IProvider
{
    public string ProviderId => providerConfig.ProviderId;
    public ProviderType ProviderType => ProviderType.Adyen;

    public async Task<Result<ProviderResponse>> AuthorizeAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Adyen authorize request for payment {PaymentId}", request.PaymentId);

        // Placeholder implementation
        await Task.Delay(100, cancellationToken);

        return new ProviderResponse(
            $"adyen_{Guid.NewGuid()}",
            ProviderId,
            true,
            null, null, new Dictionary<string, string>
            {
                { "status", "authorised" }
            }
        );
    }

    public async Task<Result<ProviderResponse>> CaptureAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Adyen capture request for payment {PaymentId}", request.PaymentId);

        if (string.IsNullOrEmpty(request.AuthorizationToken))
        {
            return ProviderErrors.MissingAuthorizationToken;
        }

        // Placeholder implementation
        await Task.Delay(100, cancellationToken);

        return new ProviderResponse(
            $"adyen_{Guid.NewGuid()}",
            ProviderId,
            true,
            null, null, new Dictionary<string, string>
            {
                { "status", "captured" }
            }
        );
    }

    public async Task<Result<ProviderResponse>> RefundAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Adyen refund request for payment {PaymentId}", request.PaymentId);

        if (string.IsNullOrEmpty(request.AuthorizationToken))
        {
            return ProviderErrors.MissingAuthorizationToken;
        }

        // Placeholder implementation
        await Task.Delay(100, cancellationToken);

        return new ProviderResponse(
            $"adyen_{Guid.NewGuid()}",
            ProviderId,
            true,
            null, null, new Dictionary<string, string>
            {
                { "status", "refunded" }
            }
        );
    }

    public async Task<Result<ProviderResponse>> VoidAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Adyen void request for payment {PaymentId}", request.PaymentId);

        if (string.IsNullOrEmpty(request.AuthorizationToken))
        {
            return ProviderErrors.MissingAuthorizationToken;
        }

        // Placeholder implementation
        await Task.Delay(100, cancellationToken);

        return new ProviderResponse(
            $"adyen_{Guid.NewGuid()}",
            ProviderId,
            true,
            null, null, new Dictionary<string, string>
            {
                { "status", "cancelled" }
            }
        );
    }

    public async Task<Result<ProviderHealthStatus>> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Checking Adyen provider health");

            // Placeholder health check
            await Task.Delay(100, cancellationToken);

            return new ProviderHealthStatus(
                true,
                Message: "Adyen API is reachable"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Adyen health check failed");
            return new ProviderHealthStatus(
                false,
                Message: ex.Message
            );
        }
    }

    public bool SupportsOperation(Operation operation)
    {
        return providerConfig.SupportsOperation(operation);
    }
}

/// <summary>
/// Bank provider implementation
/// </summary>
public sealed class BankProvider(
    Provider providerConfig,
    HttpClient httpClient,
    ILogger<BankProvider> logger) : IProvider
{
    public string ProviderId => providerConfig.ProviderId;
    public ProviderType ProviderType => ProviderType.Bank;

    public async Task<Result<ProviderResponse>> AuthorizeAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Bank authorize request for payment {PaymentId}", request.PaymentId);

        // Placeholder implementation
        await Task.Delay(100, cancellationToken);

        return new ProviderResponse(
            $"bank_{Guid.NewGuid()}",
            ProviderId,
            true,
            null, null, new Dictionary<string, string>
            {
                { "status", "authorized" }
            }
        );
    }

    public async Task<Result<ProviderResponse>> CaptureAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Bank capture request for payment {PaymentId}", request.PaymentId);

        if (string.IsNullOrEmpty(request.AuthorizationToken))
        {
            return ProviderErrors.MissingAuthorizationToken;
        }

        // Placeholder implementation
        await Task.Delay(100, cancellationToken);

        return new ProviderResponse(
            $"bank_{Guid.NewGuid()}",
            ProviderId,
            true,
            null, null, new Dictionary<string, string>
            {
                { "status", "captured" }
            }
        );
    }

    public async Task<Result<ProviderResponse>> RefundAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Bank refund request for payment {PaymentId}", request.PaymentId);

        if (string.IsNullOrEmpty(request.AuthorizationToken))
        {
            return ProviderErrors.MissingAuthorizationToken;
        }

        // Placeholder implementation
        await Task.Delay(100, cancellationToken);

        return new ProviderResponse(
            $"bank_{Guid.NewGuid()}",
            ProviderId,
            true,
            null, null, new Dictionary<string, string>
            {
                { "status", "refunded" }
            }
        );
    }

    public async Task<Result<ProviderResponse>> VoidAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Bank void request for payment {PaymentId}", request.PaymentId);

        if (string.IsNullOrEmpty(request.AuthorizationToken))
        {
            return ProviderErrors.MissingAuthorizationToken;
        }

        // Placeholder implementation
        await Task.Delay(100, cancellationToken);

        return new ProviderResponse(
            $"bank_{Guid.NewGuid()}",
            ProviderId,
            true,
            null, null, new Dictionary<string, string>
            {
                { "status", "voided" }
            }
        );
    }

    public async Task<Result<ProviderHealthStatus>> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Checking Bank provider health");

            // Placeholder health check
            await Task.Delay(100, cancellationToken);

            return new ProviderHealthStatus(
                true,
                Message: "Bank API is reachable"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bank health check failed");
            return new ProviderHealthStatus(
                false,
                Message: ex.Message
            );
        }
    }

    public bool SupportsOperation(Operation operation)
    {
        return providerConfig.SupportsOperation(operation);
    }
}

/// <summary>
/// Wallet provider implementation
/// </summary>
public sealed class WalletProvider(
    Provider providerConfig,
    HttpClient httpClient,
    ILogger<WalletProvider> logger) : IProvider
{
    public string ProviderId => providerConfig.ProviderId;
    public ProviderType ProviderType => ProviderType.Wallet;

    public async Task<Result<ProviderResponse>> AuthorizeAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Wallet authorize request for payment {PaymentId}", request.PaymentId);

        // Placeholder implementation
        await Task.Delay(100, cancellationToken);

        return new ProviderResponse(
            $"wallet_{Guid.NewGuid()}",
            ProviderId,
            true,
            null, null, new Dictionary<string, string>
            {
                { "status", "authorized" }
            }
        );
    }

    public async Task<Result<ProviderResponse>> CaptureAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Wallet capture request for payment {PaymentId}", request.PaymentId);

        if (string.IsNullOrEmpty(request.AuthorizationToken))
        {
            return ProviderErrors.MissingAuthorizationToken;
        }

        // Placeholder implementation
        await Task.Delay(100, cancellationToken);

        return new ProviderResponse(
            $"wallet_{Guid.NewGuid()}",
            ProviderId,
            true,
            null, null, new Dictionary<string, string>
            {
                { "status", "captured" }
            }
        );
    }

    public async Task<Result<ProviderResponse>> RefundAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Wallet refund request for payment {PaymentId}", request.PaymentId);

        if (string.IsNullOrEmpty(request.AuthorizationToken))
        {
            return ProviderErrors.MissingAuthorizationToken;
        }

        // Placeholder implementation
        await Task.Delay(100, cancellationToken);

        return new ProviderResponse(
            $"wallet_{Guid.NewGuid()}",
            ProviderId,
            true,
            null, null, new Dictionary<string, string>
            {
                { "status", "refunded" }
            }
        );
    }

    public async Task<Result<ProviderResponse>> VoidAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Wallet void request for payment {PaymentId}", request.PaymentId);

        if (string.IsNullOrEmpty(request.AuthorizationToken))
        {
            return ProviderErrors.MissingAuthorizationToken;
        }

        // Placeholder implementation
        await Task.Delay(100, cancellationToken);

        return new ProviderResponse(
            $"wallet_{Guid.NewGuid()}",
            ProviderId,
            true,
            null, null, new Dictionary<string, string>
            {
                { "status", "cancelled" }
            }
        );
    }

    public async Task<Result<ProviderHealthStatus>> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Checking Wallet provider health");

            // Placeholder health check
            await Task.Delay(100, cancellationToken);

            return new ProviderHealthStatus(
                true,
                Message: "Wallet API is reachable"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Wallet health check failed");
            return new ProviderHealthStatus(
                false,
                Message: ex.Message
            );
        }
    }

    public bool SupportsOperation(Operation operation)
    {
        return providerConfig.SupportsOperation(operation);
    }
}
