namespace Authentication.Features.Login;

/// <summary>
/// Login response model
/// </summary>
public record LoginResponse(
    bool Success,
    string? AccessToken = null,
    string? RefreshToken = null,
    string? TokenType = null,
    int? ExpiresIn = null,
    string? Error = null,
    string? ErrorDescription = null
)
{
    public static LoginResponse Successful(string accessToken, int expiresIn)
        => new(true, accessToken, null, "Bearer", expiresIn);

    public static LoginResponse Failed(string error, string errorDescription)
        => new(false, Error: error, ErrorDescription: errorDescription);
}
