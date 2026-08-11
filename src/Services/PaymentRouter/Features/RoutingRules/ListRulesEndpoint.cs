using Microsoft.AspNetCore.Mvc;
using PaymentRouter.Features.Shared.Routes;
using PaymentRouter.Shared;

namespace PaymentRouter.Features.RoutingRules;

/// <summary>
/// API endpoint to list routing rules
/// </summary>
public sealed class ListRulesApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet(RouteConsts.BaseRoute + "/rules", Handle)
            .WithName("ListRules")
            .WithOpenApi()
            .WithTags("Routing Rules")
            .WithSummary("List routing rules")
            .WithDescription("Retrieves a list of routing rules, optionally filtered by merchant or active status");
    }

    private static async Task<IResult> Handle(
        [FromQuery] string? merchantId,
        [FromQuery] bool? isActive,
        IListRulesHandler handler,
        CancellationToken cancellationToken)
    {
        var request = new ListRulesRequest(merchantId, isActive);
        var response = await handler.HandleAsync(request, cancellationToken);

        if (response.IsError)
        {
            return response.ToProblem();
        }

        return Results.Ok(response.Value);
    }
}
