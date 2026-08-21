using Microsoft.AspNetCore.Identity;

namespace Identity.Domain;

/// <summary>
/// Represents a role in the identity system
/// </summary>
public class IdentityRole : IdentityRole<Guid>
{
    /// <summary>
    /// Gets or sets the description of the role
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the updated timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
