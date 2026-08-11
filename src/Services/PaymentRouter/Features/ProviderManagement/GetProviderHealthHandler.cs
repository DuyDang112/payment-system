using Microsoft.EntityFrameworkCore;
using PaymentRouter.Domain.Models;
using PaymentRouter.Features.Shared.Errors;
using PaymentRouter.Infrastructure.Data;
using PaymentRouter.Shared;

namespace PaymentRouter.Features.ProviderManagement;

internal sealed class GetProviderHealthHandler(
    PaymentRouterDbContext context,
    ILogger<GetProviderHealthHandler> logger) : IGetProviderHealthHandler
{
    public async Task<Result<GetProviderHealthResponse>> HandleAsync(
        GetProviderHealthRequest request,
        CancellationToken cancellationToken)
    {
        var provider = await context.PaymentProviders
            .FirstOrDefaultAsync(p => p.ProviderId == request.ProviderId, cancellationToken);

        if (provider == null)
        {
            logger.LogWarning("Provider {ProviderId} not found", request.ProviderId);
            return Result<GetProviderHealthResponse>.Failure(RoutingErrors.ProviderNotFound);
        }

        var metrics = provider.GetPerformanceMetrics();

        var response = new GetProviderHealthResponse(
            provider.ProviderId,
            provider.ProviderName,
            provider.HealthStatus,
            provider.CircuitBreakerState,
            provider.LastHealthCheck,
            new PerformanceHealthDto(
                metrics.SuccessRate,
                metrics.FailureRate,
                metrics.ConsecutiveFailures,
                metrics.P50Latency,
                metrics.P95Latency,
                metrics.P99Latency
            )
        );

        logger.LogInformation("Retrieved health for provider {ProviderId}", request.ProviderId);

        return Result<GetProviderHealthResponse>.Success(response);
    }
}
