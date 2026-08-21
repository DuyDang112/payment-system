namespace Authentication.Features.Token;

/// <summary>
/// Token response model
/// </summary>
public record TokenResponse(
    bool Success,
    string? AccessToken = null,
    string? TokenType = null,
    int? ExpiresIn = null,
    string? RefreshToken = null,
    string? Scope = null,
    string? IdToken = null,
    string? Error = null,
    string? ErrorDescription = null
)
{
    public static TokenResponse Successful(string accessToken, string tokenType, int expiresIn, string? refreshToken = null, string? scope = null, string? idToken = null)
        => new(true, accessToken, tokenType, expiresIn, refreshToken, scope, idToken);

    public static TokenResponse Failed(string error, string errorDescription)
        => new(false, Error: error, ErrorDescription: errorDescription);
}
