using Identity.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Features.Token;

/// <summary>
/// Endpoint for handling token requests (authorization code, client credentials, refresh token)
/// </summary>
public class TokenEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/connect/token", HandleTokenAsync)
            .WithName("Token")
            .WithTags("Token")
            .AllowAnonymous();

        app.MapPost("/connect/token/refresh", HandleRefreshTokenAsync)
            .WithName("RefreshToken")
            .WithTags("Token")
            .AllowAnonymous();
    }

    private async Task<IResult> HandleTokenAsync(
        [FromForm] string? grantType,
        [FromForm] string? code,
        [FromForm] string? redirectUri,
        [FromForm] string? clientId,
        [FromForm] string? clientSecret,
        [FromForm] string? scope,
        [FromForm] string? username,
        [FromForm] string? password,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // This is handled by Duende IdentityServer middleware
        // This endpoint is just for documentation and routing purposes
        await Task.CompletedTask;
        return Results.Ok(new
        {
            message = "Token requests are handled by Duende IdentityServer middleware",
            grantType,
            clientId
        });
    }

    private async Task<IResult> HandleRefreshTokenAsync(
        [FromForm] string? refreshToken,
        [FromForm] string? clientId,
        [FromForm] string? clientSecret,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // This is handled by Duende IdentityServer middleware
        // This endpoint is just for documentation and routing purposes
        await Task.CompletedTask;
        return Results.Ok(new
        {
            message = "Token refresh is handled by Duende IdentityServer middleware",
            clientId
        });
    }
}
