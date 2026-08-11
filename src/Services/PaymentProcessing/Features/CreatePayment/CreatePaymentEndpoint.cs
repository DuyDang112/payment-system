using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PaymentProcessing.Features.Shared.Routes;
using PaymentProcessing.Shared;

namespace PaymentProcessing.Features.CreatePayment;

/// <summary>
/// API endpoint for creating payments
/// </summary>
public sealed class CreatePaymentApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost(RouteConsts.BaseRoute, Handle)
            .WithName("CreatePayment")
            .WithOpenApi()
            .WithTags("Payments")
            .WithSummary("Create a new payment")
            .WithDescription("Creates a new payment with idempotency support");
    }

    private static async Task<IResult> Handle(
        [FromBody] CreatePaymentRequest request,
        IValidator<CreatePaymentRequest> validator,
        ICreatePaymentHandler handler,
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
            return Result.Failure(response.Errors).ToProblem();
        }

        return Results.Created($"{RouteConsts.BaseRoute}/{response.Value!.PaymentId}", response.Value);
    }
}
