using Microsoft.AspNetCore.Mvc;
using PaymentRouter.Features.Shared.Routes;
using PaymentRouter.Shared;

namespace PaymentRouter.Features.ProviderManagement;

/// <summary>
/// API endpoint to list payment providers
/// </summary>
public sealed class ListProvidersApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet(RouteConsts.BaseRoute + "/providers", Handle)
            .WithName("ListProviders")
            .WithOpenApi()
            .WithTags("Providers")
            .WithSummary("List all payment providers")
            .WithDescription("Retrieves a list of all payment providers with their current status and performance metrics");
    }

    private static async Task<IResult> Handle(
        [FromQuery] bool? isEnabled,
        [FromQuery] string? providerType,
        IListProvidersHandler handler,
        CancellationToken cancellationToken)
    {
        var request = new ListProvidersRequest(isEnabled, providerType);
        var response = await handler.HandleAsync(request, cancellationToken);

        if (response.IsError)
        {
            return response.ToProblem();
        }

        return Results.Ok(response.Value);
    }
}
