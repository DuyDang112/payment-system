using Authentication.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Authentication.Features.UserInfo;

/// <summary>
/// UserInfo endpoint for OpenID Connect
/// </summary>
public class UserInfoEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/connect/userinfo", async (
            HttpContext httpContext,
            [FromServices] IUserInfoHandler handler,
            CancellationToken cancellationToken) =>
        {
            // Extract user ID from JWT claims
            var userIdClaim = httpContext.User.FindFirst("sub") ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Results.Unauthorized();
            }

            var request = new UserInfoRequest(userIdClaim.Value);
            var result = await handler.HandleAsync(request, cancellationToken);

            if (!result.Success)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                sub = result.Sub,
                username = result.Username,
                email = result.Email,
                name = result.Name,
                given_name = result.FirstName,
                family_name = result.LastName,
                email_verified = result.EmailVerified,
                roles = result.Roles,
                claims = result.Claims
            });
        })
        .WithName("UserInfo")
        .WithTags("Authentication")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "OpenID Connect UserInfo Endpoint",
            Description = "Returns user information for the authenticated user"
        })
        .RequireAuthorization(); // Requires valid JWT token
    }
}
