namespace PaymentRouter.Features.ProviderManagement;

/// <summary>
/// Request to get provider health
/// </summary>
public sealed record GetProviderHealthRequest(string ProviderId);
