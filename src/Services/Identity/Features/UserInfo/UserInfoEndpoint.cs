using Identity.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.Features.UserInfo;

/// <summary>
/// Endpoint for retrieving user information
/// </summary>
public class UserInfoEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/connect/userinfo", HandleUserInfoAsync)
            .WithName("UserInfo")
            .WithTags("UserInfo")
            .RequireAuthorization();

        app.MapGet("/connect/userinfo/claims", HandleUserClaimsAsync)
            .WithName("UserClaims")
            .WithTags("UserInfo")
            .RequireAuthorization();
    }

    private async Task<IResult> HandleUserInfoAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var user = httpContext.User;
        await Task.CompletedTask;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return Results.Unauthorized();
        }

        var userInfo = new
        {
            sub = user.FindFirst("sub")?.Value,
            name = user.FindFirst("name")?.Value,
            given_name = user.FindFirst("given_name")?.Value,
            family_name = user.FindFirst("family_name")?.Value,
            email = user.FindFirst("email")?.Value,
            email_verified = bool.TryParse(user.FindFirst("email_verified")?.Value, out var verified) && verified
        };

        return Results.Ok(userInfo);
    }

    private async Task<IResult> HandleUserClaimsAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var user = httpContext.User;
        await Task.CompletedTask;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return Results.Unauthorized();
        }

        var claims = user.Claims.Select(c => new
        {
            type = c.Type,
            value = c.Value
        });

        return Results.Ok(new
        {
            claims,
            authenticationType = user.Identity.AuthenticationType,
            isAuthenticated = user.Identity.IsAuthenticated
        });
    }
}
