using BankAdapter.Features.Shared.Errors;
using BankAdapter.Features.Shared.Routes;
using BankAdapter.Infrastructure.Events;
using BankAdapter.Infrastructure.Providers;
using BankAdapter.Shared;

namespace BankAdapter.Features.HealthCheck;

/// <summary>
/// Handler for provider health check requests
/// </summary>
internal sealed class HealthCheckHandler(
    ProviderFactory providerFactory,
    IEventPublisher eventPublisher,
    ILogger<HealthCheckHandler> logger) : IHealthCheckHandler
{
    public async Task<Result<HealthCheckResponse>> HandleAsync(
        string providerId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing health check request for provider {ProviderId}", providerId);

        // Get provider
        var providerResult = await providerFactory.GetProviderAsync(providerId, cancellationToken);
        if (providerResult.IsError)
        {
            return Result<HealthCheckResponse>.Failure(providerResult.Errors);
        }

        var provider = providerResult.Value;

        // Check provider health
        var healthResult = await provider.CheckHealthAsync(cancellationToken);

        if (healthResult.IsError)
        {
            // Publish health change event
            await eventPublisher.PublishAsync(new ProviderHealthChangedEvent(
                providerId,
                false,
                healthResult.Errors.FirstOrDefault()?.Message ?? "Health check failed",
                DateTime.UtcNow
            ), cancellationToken);

            return Result<HealthCheckResponse>.Failure(healthResult.Errors);
        }

        var healthStatus = healthResult.Value;

        // Publish health change event if status changed
        await eventPublisher.PublishAsync(new ProviderHealthChangedEvent(
            providerId,
            healthStatus.IsHealthy,
            healthStatus.Message,
            DateTime.UtcNow
        ), cancellationToken);

        logger.LogInformation(
            "Health check completed for provider {ProviderId}: {IsHealthy}",
            providerId,
            healthStatus.IsHealthy);

        return Result<HealthCheckResponse>.Success(new HealthCheckResponse(
            providerId,
            healthStatus.IsHealthy,
            healthStatus.Message,
            healthStatus.Details));
    }
}
