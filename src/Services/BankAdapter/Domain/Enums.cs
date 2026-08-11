namespace BankAdapter.Domain;

/// <summary>
/// Supported operation types for payment providers
/// </summary>
public enum Operation
{
    Authorize,
    Capture,
    Refund,
    Void
}

/// <summary>
/// Authentication types for provider integrations
/// </summary>
public enum AuthType
{
    ApiKey,
    OAuth,
    MutualTls
}

/// <summary>
/// Result types for provider requests
/// </summary>
public enum ProviderResult
{
    Success,
    RetryableError,
    FatalError
}

/// <summary>
/// Provider types supported by the Bank Adapter
/// </summary>
public enum ProviderType
{
    Stripe,
    Adyen,
    Bank,
    Wallet
}
