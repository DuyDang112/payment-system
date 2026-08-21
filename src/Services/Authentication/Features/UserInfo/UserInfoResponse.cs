namespace Authentication.Features.UserInfo;

/// <summary>
/// User info response model
/// </summary>
public record UserInfoResponse(
    bool Success,
    string? Sub = null,
    string? Username = null,
    string? Email = null,
    string? Name = null,
    string? FirstName = null,
    string? LastName = null,
    bool? EmailVerified = null,
    IEnumerable<string>? Roles = null,
    Dictionary<string, string>? Claims = null,
    string? Error = null
)
{
    public static UserInfoResponse Successful(
        string sub,
        string username,
        string email,
        string? firstName,
        string? lastName,
        bool emailVerified,
        IEnumerable<string> roles,
        Dictionary<string, string> claims)
    {
        var name = $"{firstName} {lastName}".Trim();
        return new UserInfoResponse(true, sub, username, email, name, firstName, lastName, emailVerified, roles, claims);
    }

    public static UserInfoResponse Failed(string error)
        => new(false, Error: error);
}
