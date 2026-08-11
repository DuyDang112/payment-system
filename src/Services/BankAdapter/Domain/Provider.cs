using System.Text.Json.Serialization;

namespace BankAdapter.Domain;

/// <summary>
/// Represents a payment provider integration configuration
/// </summary>
public sealed class Provider
{
    public string IntegrationId { get; private set; } = string.Empty;
    public string ProviderId { get; private set; } = string.Empty;
    public ProviderType ProviderType { get; private set; }
    public string ApiVersion { get; private set; } = string.Empty;
    public string Endpoint { get; private set; } = string.Empty;
    public AuthConfig AuthConfig { get; private set; } = AuthConfig.Default();
    public RateLimits RateLimits { get; private set; } = RateLimits.Default();
    public RetryConfig RetryConfig { get; private set; } = RetryConfig.Default();
    public Timeouts Timeouts { get; private set; } = Timeouts.Default();
    public HashSet<Operation> SupportedOperations { get; private set; } = new();
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// EF Core constructor
    /// </summary>
    private Provider() { }

    /// <summary>
    /// Create a new provider integration
    /// </summary>
    public static Provider Create(
        string providerId,
        ProviderType providerType,
        string apiVersion,
        string endpoint,
        AuthConfig authConfig,
        RateLimits? rateLimits = null,
        RetryConfig? retryConfig = null,
        Timeouts? timeouts = null,
        HashSet<Operation>? supportedOperations = null)
    {
        var provider = new Provider
        {
            IntegrationId = Guid.NewGuid().ToString(),
            ProviderId = providerId,
            ProviderType = providerType,
            ApiVersion = apiVersion,
            Endpoint = endpoint,
            AuthConfig = authConfig,
            RateLimits = rateLimits ?? RateLimits.Default(),
            RetryConfig = retryConfig ?? RetryConfig.Default(),
            Timeouts = timeouts ?? Timeouts.Default(),
            SupportedOperations = supportedOperations ?? new HashSet<Operation>(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return provider;
    }

    /// <summary>
    /// Update provider configuration
    /// </summary>
    public void Update(
        string? endpoint = null,
        AuthConfig? authConfig = null,
        RateLimits? rateLimits = null,
        RetryConfig? retryConfig = null,
        Timeouts? timeouts = null,
        HashSet<Operation>? supportedOperations = null)
    {
        if (endpoint != null) Endpoint = endpoint;
        if (authConfig != null) AuthConfig = authConfig;
        if (rateLimits != null) RateLimits = rateLimits;
        if (retryConfig != null) RetryConfig = retryConfig;
        if (timeouts != null) Timeouts = timeouts;
        if (supportedOperations != null) SupportedOperations = supportedOperations;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivate provider
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activate provider
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if operation is supported
    /// </summary>
    public bool SupportsOperation(Operation operation)
    {
        return SupportedOperations.Contains(operation);
    }
}

/// <summary>
/// Authentication configuration for provider
/// </summary>
[JsonSerializable(typeof(AuthConfig))]
public sealed record AuthConfig(
    AuthType Type,
    Dictionary<string, string> Credentials
)
{
    public static AuthConfig Default() => new(AuthType.ApiKey, new Dictionary<string, string>());
}

/// <summary>
/// Rate limits for provider requests
/// </summary>
public sealed record RateLimits(
    int MaxRequestsPerSecond,
    int MaxConcurrentRequests
)
{
    public static RateLimits Default() => new(100, 10);
}

/// <summary>
/// Retry configuration for provider requests
/// </summary>
public sealed record RetryConfig(
    int MaxRetries,
    int BackoffMs,
    HashSet<string> RetryableErrors
)
{
    public static RetryConfig Default() => new(3, 1000, new HashSet<string> { "timeout", "rate_limit_exceeded", "transient_error" });
}

/// <summary>
/// Timeout configuration for provider requests
/// </summary>
public sealed record Timeouts(
    int ConnectTimeoutMs,
    int ReadTimeoutMs
)
{
    public static Timeouts Default() => new(5000, 30000);
}
