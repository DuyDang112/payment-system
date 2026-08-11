using Microsoft.EntityFrameworkCore;
using PaymentRouter.Domain.Models;
using PaymentRouter.Features.Shared.Errors;
using PaymentRouter.Infrastructure.Data;
using PaymentRouter.Shared;

namespace PaymentRouter.Features.ProviderManagement;

internal sealed class AddProviderHandler(
    PaymentRouterDbContext context,
    ILogger<AddProviderHandler> logger) : IAddProviderHandler
{
    public async Task<Result<AddProviderResponse>> HandleAsync(
        AddProviderRequest request,
        CancellationToken cancellationToken)
    {
        // Check if provider already exists
        var exists = await context.PaymentProviders
            .AnyAsync(p => p.ProviderId == request.ProviderId, cancellationToken);

        if (exists)
        {
            logger.LogWarning("Provider {ProviderId} already exists", request.ProviderId);
            return Result<AddProviderResponse>.Failure(
                new Error("Routing.ProviderExists", $"Provider {request.ProviderId} already exists"));
        }

        // Create cost configuration
        var costConfig = new CostConfiguration(
            new Money(request.FixedFee.Amount, request.FixedFee.Currency),
            request.PercentageFee,
            request.MinFee != null ? new Money(request.MinFee.Amount, request.MinFee.Currency) : null,
            request.MaxFee != null ? new Money(request.MaxFee.Amount, request.MaxFee.Currency) : null
        );

        // Create provider
        var provider = PaymentProvider.Create(
            request.ProviderId,
            request.ProviderName,
            request.ProviderType,
            request.SupportedCurrencies,
            request.SupportedMethods,
            request.Priority,
            costConfig
        );

        // Update limits
        // Note: Need to add these properties to PaymentProvider entity

        await context.PaymentProviders.AddAsync(provider, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created provider {ProviderId} - {ProviderName}", provider.ProviderId, provider.ProviderName);

        return Result<AddProviderResponse>.Success(new AddProviderResponse(
            provider.ProviderId,
            provider.ProviderName,
            provider.IsEnabled
        ));
    }
}
