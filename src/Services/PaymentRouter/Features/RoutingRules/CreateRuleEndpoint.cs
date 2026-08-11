using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PaymentRouter.Features.Shared.Routes;
using PaymentRouter.Shared;

namespace PaymentRouter.Features.RoutingRules;

/// <summary>
/// API endpoint to create a routing rule
/// </summary>
public sealed class CreateRuleApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost(RouteConsts.BaseRoute + "/rules", Handle)
            .WithName("CreateRule")
            .WithOpenApi()
            .WithTags("Routing Rules")
            .WithSummary("Create a routing rule")
            .WithDescription("Creates a new routing rule for intelligent payment routing");
    }

    private static async Task<IResult> Handle(
        [FromBody] CreateRuleRequest request,
        IValidator<CreateRuleRequest> validator,
        ICreateRuleHandler handler,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var response = await handler.HandleAsync(request, cancellationToken);
        if (response.IsError)
        {
            return response.ToProblem();
        }

        return Results.Created($"/api/routing/rules/{response.Value.RuleId}", response.Value);
    }
}
