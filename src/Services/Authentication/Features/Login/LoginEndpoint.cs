using Authentication.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Features.Login;

/// <summary>
/// Login endpoint
/// </summary>
public class LoginEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/api/auth/login", async (
            [FromBody] LoginRequest request,
            [FromServices] ILoginHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(request, cancellationToken);

            if (!result.Success)
            {
                return Results.Problem(
                    detail: result.ErrorDescription,
                    statusCode: 401,
                    title: result.Error
                );
            }

            return Results.Ok(new
            {
                access_token = result.AccessToken,
                refresh_token = result.RefreshToken,
                token_type = result.TokenType,
                expires_in = result.ExpiresIn
            });
        })
        .WithName("Login")
        .WithTags("Authentication")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Authenticate user",
            Description = "Authenticates a user with username/password and returns access token"
        });
    }
}
