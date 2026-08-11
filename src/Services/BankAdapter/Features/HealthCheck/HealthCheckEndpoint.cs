using Microsoft.AspNetCore.Mvc;
using BankAdapter.Features.Shared.Routes;
using BankAdapter.Shared;

namespace BankAdapter.Features.HealthCheck;

/// <summary>
/// API endpoint for provider health check
/// </summary>
public sealed class HealthCheckApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet(RouteConsts.HealthCheck, Handle)
            .WithName("HealthCheck")
            .WithOpenApi()
            .WithTags("BankAdapter")
            .WithSummary("Check provider health")
            .WithDescription("Checks the health status of a specific payment provider");
    }

    private static async Task<IResult> Handle(
        [FromRoute] string id,
        IHealthCheckHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, cancellationToken);
        if (response.IsError)
        {
            return response.Errors.ToProblem();
        }

        return Results.Ok(response.Value);
    }
}
