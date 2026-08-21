namespace Authentication.Features.Register;

/// <summary>
/// Registration response model
/// </summary>
public record RegisterResponse(
    bool Success,
    Guid? UserId = null,
    string? Username = null,
    string? Email = null,
    string? Error = null,
    string? ErrorDescription = null
)
{
    public static RegisterResponse Successful(Guid userId, string username, string email)
        => new(true, userId, username, email);

    public static RegisterResponse Failed(string error, string errorDescription)
        => new(false, Error: error, ErrorDescription: errorDescription);
}
