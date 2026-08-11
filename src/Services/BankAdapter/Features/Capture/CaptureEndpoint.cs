using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using BankAdapter.Features.Shared.Routes;
using BankAdapter.Shared;

namespace BankAdapter.Features.Capture;

/// <summary>
/// API endpoint for payment capture
/// </summary>
public sealed class CaptureApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost(RouteConsts.Capture, Handle)
            .WithName("CapturePayment")
            .WithOpenApi()
            .WithTags("BankAdapter")
            .WithSummary("Capture a previously authorized payment")
            .WithDescription("Processes a payment capture request for a previously authorized payment");
    }

    private static async Task<IResult> Handle(
        [FromBody] CaptureRequest request,
        IValidator<CaptureRequest> validator,
        ICaptureHandler handler,
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
