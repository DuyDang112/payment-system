using Identity.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Features.Authorize;

/// <summary>
/// Endpoint for handling authorization code flow
/// </summary>
public class AuthorizeEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/connect/authorize", HandleAuthorizeAsync)
            .WithName("Authorize")
            .WithTags("Authorization");

        app.MapPost("/connect/authorize", HandleAuthorizeAsync)
            .WithName("AuthorizePost")
            .WithTags("Authorization");
    }

    private async Task<IResult> HandleAuthorizeAsync(
        [FromQuery] string? responseType,
        [FromQuery] string? clientId,
        [FromQuery] string? redirectUri,
        [FromQuery] string? scope,
        [FromQuery] string? state,
        [FromQuery] string? codeChallenge,
        [FromQuery] string? codeChallengeMethod,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // This is handled by Duende IdentityServer middleware
        // This endpoint is just for documentation and routing purposes
        await Task.CompletedTask;
        return Results.Ok(new
        {
            message = "Authorization is handled by Duende IdentityServer middleware",
            responseType,
            clientId,
            redirectUri,
            scope,
            state
        });
    }
}
