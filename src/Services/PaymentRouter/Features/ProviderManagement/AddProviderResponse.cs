namespace PaymentRouter.Features.ProviderManagement;

/// <summary>
/// Response after adding a payment provider
/// </summary>
public sealed record AddProviderResponse(
    string ProviderId,
    string ProviderName,
    bool IsEnabled
);
