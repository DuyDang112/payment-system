using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PaymentRouter.Features.Shared.Routes;
using PaymentRouter.Shared;

namespace PaymentRouter.Features.ProviderManagement;

/// <summary>
/// API endpoint to add a payment provider
/// </summary>
public sealed class AddProviderApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost(RouteConsts.BaseRoute + "/providers", Handle)
            .WithName("AddProvider")
            .WithOpenApi()
            .WithTags("Providers")
            .WithSummary("Add a new payment provider")
            .WithDescription("Adds a new payment provider to the routing system");
    }

    private static async Task<IResult> Handle(
        [FromBody] AddProviderRequest request,
        IValidator<AddProviderRequest> validator,
        IAddProviderHandler handler,
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

        return Results.Created($"/api/routing/providers/{response.Value.ProviderId}", response.Value);
    }
}
