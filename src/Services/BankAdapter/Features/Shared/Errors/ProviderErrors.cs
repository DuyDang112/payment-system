using BankAdapter.Shared;

namespace BankAdapter.Features.Shared.Errors;

/// <summary>
/// Provider-specific error definitions
/// </summary>
public static class ProviderErrors
{
    public static readonly Error ProviderNotFound = new("Provider.NotFound", "Provider not found or inactive");
    public static readonly Error OperationNotSupported = new("Provider.OperationNotSupported", "Operation not supported by provider");
    public static readonly Error ProviderTimeout = new("Provider.Timeout", "Provider request timed out");
    public static readonly Error ProviderRateLimitExceeded = new("Provider.RateLimitExceeded", "Provider rate limit exceeded");
    public static readonly Error MissingAuthorizationToken = new("Provider.MissingAuthorizationToken", "Authorization token is required for this operation");
    public static readonly Error InvalidRequest = new("Provider.InvalidRequest", "Invalid provider request");
    public static readonly Error AuthenticationFailed = new("Provider.AuthenticationFailed", "Provider authentication failed");

    public static Error ProviderError(string providerId, string errorCode, string errorMessage)
        => new($"Provider.{providerId}.{errorCode}", errorMessage);

    public static Error ProviderUnavailable(string providerId)
        => new($"Provider.{providerId}.Unavailable", $"Provider {providerId} is currently unavailable");

    public static Error ConfigurationError(string message)
        => new("Provider.ConfigurationError", message);

    public static Error SerializationError(string message)
        => new("Provider.SerializationError", message);
}
