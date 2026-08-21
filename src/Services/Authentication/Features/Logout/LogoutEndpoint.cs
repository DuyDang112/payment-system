using Authentication.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Features.Logout;

/// <summary>
/// Logout endpoint
/// </summary>
public class LogoutEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/connect/logout", async (
            [FromBody] LogoutRequest? request,
            [FromServices] ILogoutHandler handler,
            CancellationToken cancellationToken) =>
        {
            var logoutRequest = request ?? new LogoutRequest(null, null);
            var result = await handler.HandleAsync(logoutRequest, cancellationToken);

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
                message = "Successfully logged out",
                post_logout_redirect_uri = result.PostLogoutRedirectUri
            });
        })
        .WithName("Logout")
        .WithTags("Authentication")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Logout endpoint",
            Description = "Logs out the current user and invalidates tokens"
        });
    }
}
