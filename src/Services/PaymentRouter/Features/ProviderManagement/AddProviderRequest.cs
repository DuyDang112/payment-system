using PaymentRouter.Domain.Models;

namespace PaymentRouter.Features.ProviderManagement;

/// <summary>
/// Request to add a new payment provider
/// </summary>
public sealed record AddProviderRequest(
    string ProviderId,
    string ProviderName,
    ProviderType ProviderType,
    string[] SupportedCurrencies,
    PaymentMethod[] SupportedMethods,
    int Priority,
    MoneyDto FixedFee,
    decimal PercentageFee,
    MoneyDto? MinFee = null,
    MoneyDto? MaxFee = null,
    decimal MinAmount = 0.01m,
    decimal MaxAmount = 1000000m
);

public sealed record MoneyDto(decimal Amount, string Currency);
