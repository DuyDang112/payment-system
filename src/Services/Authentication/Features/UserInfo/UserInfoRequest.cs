namespace Authentication.Features.UserInfo;

/// <summary>
/// User info request model (uses bearer token from Authorization header)
/// </summary>
public record UserInfoRequest(string? UserId);
