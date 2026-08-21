namespace Authentication.Domain;

/// <summary>
/// Role claims for role-based permissions
/// </summary>
public class RoleClaim
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public string ClaimType { get; set; } = string.Empty;
    public string ClaimValue { get; set; } = string.Empty;

    // Navigation property
    public Role Role { get; set; } = null!;
}
