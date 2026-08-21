namespace Authentication.Domain;

/// <summary>
/// Role entity for role-based authorization
/// </summary>
public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<RoleClaim> Claims { get; set; } = new List<RoleClaim>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
