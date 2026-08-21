namespace Authentication.Features.Logout;

/// <summary>
/// Logout response model
/// </summary>
public record LogoutResponse(
    bool Success,
    string? PostLogoutRedirectUri = null,
    string? Error = null,
    string? ErrorDescription = null
)
{
    public static LogoutResponse Successful(string? postLogoutRedirectUri = null)
        => new(true, postLogoutRedirectUri);

    public static LogoutResponse Failed(string error, string errorDescription)
        => new(false, Error: error, ErrorDescription: errorDescription);
}
