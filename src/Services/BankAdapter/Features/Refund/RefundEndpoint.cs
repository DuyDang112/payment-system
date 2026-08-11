using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using BankAdapter.Features.Shared.Routes;
using BankAdapter.Shared;

namespace BankAdapter.Features.Refund;

/// <summary>
/// API endpoint for payment refund
/// </summary>
public sealed class RefundApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost(RouteConsts.Refund, Handle)
            .WithName("RefundPayment")
            .WithOpenApi()
            .WithTags("BankAdapter")
            .WithSummary("Refund a payment")
            .WithDescription("Processes a payment refund request for a previously captured payment");
    }

    private static async Task<IResult> Handle(
        [FromBody] RefundRequest request,
        IValidator<RefundRequest> validator,
        IRefundHandler handler,
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
