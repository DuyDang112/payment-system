namespace Authentication.Features.Logout;

/// <summary>
/// Logout request model
/// </summary>
public record LogoutRequest(
    string? RefreshToken,
    string? PostLogoutRedirectUri
);
