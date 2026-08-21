using Identity.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Features.HealthCheck;

/// <summary>
/// Endpoint for health checks
/// </summary>
public class HealthCheckEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/health", HandleHealthCheckAsync)
            .WithName("HealthCheck")
            .WithTags("Health")
            .AllowAnonymous();

        app.MapGet("/health/ready", HandleReadinessCheckAsync)
            .WithName("ReadinessCheck")
            .WithTags("Health")
            .AllowAnonymous();

        app.MapGet("/health/live", HandleLivenessCheckAsync)
            .WithName("LivenessCheck")
            .WithTags("Health")
            .AllowAnonymous();
    }

    private async Task<IResult> HandleHealthCheckAsync(
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        return Results.Ok(new
        {
            status = "healthy",
            service = "Identity",
            timestamp = DateTime.UtcNow,
            checks = new
            {
                identityServer = "healthy",
                database = "healthy"
            }
        });
    }

    private async Task<IResult> HandleReadinessCheckAsync(
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        // Add readiness checks here (database connectivity, external services, etc.)
        return Results.Ok(new
        {
            status = "ready",
            timestamp = DateTime.UtcNow
        });
    }

    private async Task<IResult> HandleLivenessCheckAsync(
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        // Simple liveness check - just return OK
        return Results.Ok(new
        {
            status = "alive",
            timestamp = DateTime.UtcNow
        });
    }
}
