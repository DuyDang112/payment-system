using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using BankAdapter.Features.Shared.Routes;
using BankAdapter.Shared;

namespace BankAdapter.Features.Void;

/// <summary>
/// API endpoint for payment void
/// </summary>
public sealed class VoidApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost(RouteConsts.Void, Handle)
            .WithName("VoidPayment")
            .WithOpenApi()
            .WithTags("BankAdapter")
            .WithSummary("Void a payment")
            .WithDescription("Processes a payment void request to cancel a previously authorized payment");
    }

    private static async Task<IResult> Handle(
        [FromBody] VoidRequest request,
        IValidator<VoidRequest> validator,
        IVoidHandler handler,
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
            return response.Errors.ToProblem();
        }

        return Results.Ok(response.Value);
    }
}
