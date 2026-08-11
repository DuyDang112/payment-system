using Microsoft.EntityFrameworkCore;
using PaymentRouter.Domain.Models;
using PaymentRouter.Infrastructure.Data;
using PaymentRouter.Shared;

namespace PaymentRouter.Features.ProviderManagement;

internal sealed class ListProvidersHandler(
    PaymentRouterDbContext context,
    ILogger<ListProvidersHandler> logger) : IListProvidersHandler
{
    public async Task<Result<ListProvidersResponse>> HandleAsync(
        ListProvidersRequest request,
        CancellationToken cancellationToken)
    {
        var query = context.PaymentProviders.AsQueryable();

        if (request.IsEnabled.HasValue)
        {
            query = query.Where(p => p.IsEnabled == request.IsEnabled.Value);
        }

        if (!string.IsNullOrEmpty(request.ProviderType) &&
            Enum.TryParse<ProviderType>(request.ProviderType, true, out var providerType))
        {
            query = query.Where(p => p.ProviderType == providerType);
        }

        var providers = await query
            .OrderBy(p => p.Priority)
            .ToListAsync(cancellationToken);

        var providerDtos = providers.Select(p => new ProviderDto(
            p.ProviderId,
            p.ProviderName,
            p.ProviderType,
            p.SupportedCurrencies,
            p.SupportedMethods,
            p.Priority,
            p.IsEnabled,
            p.HealthStatus,
            p.CircuitBreakerState,
            MapToPerformanceMetricsDto(p.GetPerformanceMetrics())
        )).ToArray();

        logger.LogInformation("Retrieved {Count} providers", providerDtos.Length);

        return Result<ListProvidersResponse>.Success(new ListProvidersResponse(providerDtos));
    }

    private static PerformanceMetricsDto MapToPerformanceMetricsDto(PerformanceMetrics metrics) =>
        new(
            metrics.SuccessRate,
            metrics.P50Latency,
            metrics.P95Latency,
            metrics.P99Latency,
            metrics.DailyVolume,
            metrics.FailureRate,
            metrics.ConsecutiveFailures
        );
}
