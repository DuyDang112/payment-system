using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using BankAdapter.Features.Shared.Routes;
using BankAdapter.Shared;

namespace BankAdapter.Features.Authorize;

/// <summary>
/// API endpoint for payment authorization
/// </summary>
public sealed class AuthorizeApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost(RouteConsts.Authorize, Handle)
            .WithName("AuthorizePayment")
            .WithOpenApi()
            .WithTags("BankAdapter")
            .WithSummary("Authorize a payment through a provider")
            .WithDescription("Processes a payment authorization request through the specified provider");
    }

    private static async Task<IResult> Handle(
        [FromBody] AuthorizeRequest request,
        IValidator<AuthorizeRequest> validator,
        IAuthorizeHandler handler,
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
