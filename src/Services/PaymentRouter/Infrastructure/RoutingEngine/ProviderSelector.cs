using Microsoft.EntityFrameworkCore;
using PaymentRouter.Domain.Models;
using PaymentRouter.Infrastructure.CircuitBreaking;
using PaymentRouter.Infrastructure.Data;

namespace PaymentRouter.Infrastructure.RoutingEngine;

/// <summary>
/// Selects the best payment provider based on routing strategy
/// </summary>
public interface IProviderSelector
{
    Task<ProviderSelectionResult> SelectProviderAsync(
        RoutingContext context,
        CancellationToken cancellationToken);
}

public sealed record ProviderSelectionResult(
    PaymentProvider? SelectedProvider,
    string[] AlternativeProviders,
    string Reason);

public sealed record RoutingContext(
    string MerchantId,
    string PaymentId,
    Money Amount,
    string Currency,
    PaymentMethod PaymentMethod,
    string? CountryCode = null);

public sealed class ProviderSelector : IProviderSelector
{
    private readonly ICircuitBreakerManager _circuitBreakerManager;
    private readonly PaymentRouterDbContext _dbContext;
    private readonly ILogger<ProviderSelector> _logger;

    public ProviderSelector(
        ICircuitBreakerManager circuitBreakerManager,
        PaymentRouterDbContext dbContext,
        ILogger<ProviderSelector> logger)
    {
        _circuitBreakerManager = circuitBreakerManager;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ProviderSelectionResult> SelectProviderAsync(
        RoutingContext context,
        CancellationToken cancellationToken)
    {
        var providers = await GetEligibleProvidersAsync(context, cancellationToken);

        if (!providers.Any())
        {
            _logger.LogWarning("No eligible providers found for payment {PaymentId}", context.PaymentId);
            return new ProviderSelectionResult(null, Array.Empty<string>(), "No eligible providers");
        }

        var selected = providers.First();
        var alternatives = providers.Skip(1).Take(3).Select(p => p.ProviderId).ToArray();

        _logger.LogInformation(
            "Selected provider {ProviderId} for payment {PaymentId} using {Strategy} strategy",
            selected.ProviderId,
            context.PaymentId,
            "Default");

        return new ProviderSelectionResult(
            selected,
            alternatives,
            $"Selected {selected.ProviderName} ({selected.ProviderId})");
    }

    private async Task<List<PaymentProvider>> GetEligibleProvidersAsync(
        RoutingContext context,
        CancellationToken cancellationToken)
    {
        // Get all enabled providers from database
        var allProviders = await _dbContext.PaymentProviders
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Priority)
            .ToListAsync(cancellationToken);

        // Filter providers based on routing context
        var eligibleProviders = allProviders
            .Where(provider => provider.SupportsCurrency(context.Currency))
            .Where(provider => provider.SupportsMethod(context.PaymentMethod))
            .Where(provider => provider.IsWithinLimits(context.Amount.Amount))
            .Where(provider => provider.IsHealthy())
            .Where(provider => provider.IsAvailable())
            .ToList();

        _logger.LogInformation(
            "Found {EligibleCount} eligible providers out of {TotalCount} for payment {PaymentId}",
            eligibleProviders.Count,
            allProviders.Count,
            context.PaymentId);

        return eligibleProviders;
    }
}
