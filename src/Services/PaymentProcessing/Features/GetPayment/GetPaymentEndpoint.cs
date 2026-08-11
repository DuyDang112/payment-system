using Microsoft.AspNetCore.Mvc;
using PaymentProcessing.Features.Shared.Routes;
using PaymentProcessing.Shared;
using PaymentProcessing.Features.GetPayment;

namespace PaymentProcessing.Features.GetPayment;

/// <summary>
/// API endpoint for getting a payment
/// </summary>
public sealed class GetPaymentApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet(RouteConsts.GetByIdRoute, Handle)
            .WithName("GetPayment")
            .WithOpenApi()
            .WithTags("Payments")
            .WithSummary("Get a payment by ID")
            .WithDescription("Retrieves payment details including attempts");
    }

    private static async Task<IResult> Handle(
        [FromRoute] string id,
        IGetPaymentHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, cancellationToken);
        if (response.IsError)
        {
            return Result.Failure(response.Errors).ToProblem();
        }

        return Results.Ok(response.Value);
    }
}
