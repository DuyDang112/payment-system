using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PaymentProcessing.Features.Shared.Routes;
using PaymentProcessing.Shared;

namespace PaymentProcessing.Features.CancelPayment;

/// <summary>
/// API endpoint for cancelling payments
/// </summary>
public sealed class CancelPaymentApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost(RouteConsts.CancelRoute, Handle)
            .WithName("CancelPayment")
            .WithOpenApi()
            .WithTags("Payments")
            .WithSummary("Cancel a payment")
            .WithDescription("Cancels a payment in a cancellable state");
    }

    private static async Task<IResult> Handle(
        [FromRoute] string id,
        [FromBody] CancelPaymentRequest request,
        IValidator<CancelPaymentRequest> validator,
        ICancelPaymentHandler handler,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var response = await handler.HandleAsync(id, request, cancellationToken);
        if (response.IsError)
        {
            return Result.Failure(response.Errors).ToProblem();
        }

        return Results.Ok(response.Value);
    }
}
