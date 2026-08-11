namespace PaymentRouter.Features.ProviderManagement;

/// <summary>
/// Request to list payment providers
/// </summary>
public sealed record ListProvidersRequest(
    bool? IsEnabled = null,
    string? ProviderType = null
);
