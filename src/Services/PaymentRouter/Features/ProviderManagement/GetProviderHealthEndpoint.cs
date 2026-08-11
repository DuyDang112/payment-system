using Microsoft.AspNetCore.Mvc;
using PaymentRouter.Features.Shared.Routes;
using PaymentRouter.Shared;

namespace PaymentRouter.Features.ProviderManagement;

/// <summary>
/// API endpoint to get provider health
/// </summary>
public sealed class GetProviderHealthApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet(RouteConsts.BaseRoute + "/providers/{providerId}/health", Handle)
            .WithName("GetProviderHealth")
            .WithOpenApi()
            .WithTags("Providers")
            .WithSummary("Get provider health status")
            .WithDescription("Retrieves the current health status and performance metrics of a specific payment provider");
    }

    private static async Task<IResult> Handle(
        [FromRoute] string providerId,
        IGetProviderHealthHandler handler,
        CancellationToken cancellationToken)
    {
        var request = new GetProviderHealthRequest(providerId);
        var response = await handler.HandleAsync(request, cancellationToken);

        if (response.IsError)
        {
            return response.ToProblem();
        }

        return Results.Ok(response.Value);
    }
}
