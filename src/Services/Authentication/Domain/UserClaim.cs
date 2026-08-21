namespace Authentication.Domain;

/// <summary>
/// User claims for additional user information
/// </summary>
public class UserClaim
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ClaimType { get; set; } = string.Empty;
    public string ClaimValue { get; set; } = string.Empty;

    // Navigation property
    public User User { get; set; } = null!;
}
