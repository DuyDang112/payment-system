using Authentication.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Features.Register;

/// <summary>
/// Registration endpoint
/// </summary>
public class RegisterEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/api/auth/register", async (
            [FromBody] RegisterRequest request,
            [FromServices] IRegisterHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(request, cancellationToken);

            if (!result.Success)
            {
                return Results.Problem(
                    detail: result.ErrorDescription,
                    statusCode: 400,
                    title: result.Error
                );
            }

            return Results.Created($"/api/auth/users/{result.UserId}", new
            {
                user_id = result.UserId,
                username = result.Username,
                email = result.Email,
                message = "Registration successful. Please check your email to verify your account."
            });
        })
        .WithName("Register")
        .WithTags("Authentication")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Register new user",
            Description = "Registers a new user account with username, email, and password"
        });
    }
}
