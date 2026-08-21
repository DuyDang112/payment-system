using Authentication.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Features.Token;

/// <summary>
/// Token endpoint for OAuth 2.0 token requests
/// </summary>
public class TokenEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/connect/token", async (
            [FromBody] TokenRequest request,
            [FromServices] ITokenHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(request, cancellationToken);

            if (!result.Success)
            {
                return Results.BadRequest(new
                {
                    error = result.Error,
                    error_description = result.ErrorDescription
                });
            }

            return Results.Ok(new
            {
                access_token = result.AccessToken,
                token_type = result.TokenType,
                expires_in = result.ExpiresIn,
                refresh_token = result.RefreshToken,
                scope = result.Scope,
                id_token = result.IdToken
            });
        })
        .WithName("Token")
        .WithTags("Authentication")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "OAuth 2.0 Token Endpoint",
            Description = "Exchanges authorization codes or refresh tokens for access tokens"
        })
        .AllowAnonymous(); // Token endpoint must allow anonymous access
    }
}
