using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PaymentRouter.Features.Shared.Routes;
using PaymentRouter.Shared;

namespace PaymentRouter.Features.RoutePayment;

/// <summary>
/// API endpoint for payment routing
/// </summary>
public sealed class RoutePaymentApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost(RouteConsts.BaseRoute + "/route", Handle)
            .WithName("RoutePayment")
            .WithOpenApi()
            .WithTags("Routing")
            .WithSummary("Route payment to optimal provider")
            .WithDescription("Intelligently routes payment to the best provider based on cost, performance, and availability");
    }

    private static async Task<IResult> Handle(
        [FromBody] RoutePaymentRequest request,
        IValidator<RoutePaymentRequest> validator,
        IRoutePaymentHandler handler,
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

        return Results.Ok(response.Value);
    }
}
