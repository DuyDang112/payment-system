using BankAdapter.Domain;
using BankAdapter.Shared;

namespace BankAdapter.Infrastructure.Providers;

/// <summary>
/// Interface for payment provider integrations
/// </summary>
public interface IProvider
{
    /// <summary>
    /// Gets the provider ID
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Gets the provider type
    /// </summary>
    ProviderType ProviderType { get; }

    /// <summary>
    /// Process authorization request
    /// </summary>
    Task<Result<ProviderResponse>> AuthorizeAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process capture request
    /// </summary>
    Task<Result<ProviderResponse>> CaptureAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process refund request
    /// </summary>
    Task<Result<ProviderResponse>> RefundAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process void request
    /// </summary>
    Task<Result<ProviderResponse>> VoidAsync(
        ProviderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check provider health
    /// </summary>
    Task<Result<ProviderHealthStatus>> CheckHealthAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if operation is supported
    /// </summary>
    bool SupportsOperation(Operation operation);
}

/// <summary>
/// Provider request
/// </summary>
public sealed record ProviderRequest(
    string PaymentId,
    decimal Amount,
    string Currency,
    Dictionary<string, string> Metadata,
    string? AuthorizationToken = null
);

/// <summary>
/// Provider response
/// </summary>
public sealed record ProviderResponse(
    string ProviderTransactionId,
    string ProviderId,
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    Dictionary<string, string>? Metadata = null
);

/// <summary>
/// Provider health status
/// </summary>
public sealed record ProviderHealthStatus(
    bool IsHealthy,
    string? Message = null,
    Dictionary<string, string>? Details = null
);
