using Microsoft.AspNetCore.Identity;

namespace Identity.Domain;

/// <summary>
/// Represents a user in the identity system
/// </summary>
public class IdentityUser : IdentityUser<Guid>
{
    /// <summary>
    /// Gets or sets the first name of the user
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the last name of the user
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the full name of the user
    /// </summary>
    public string? FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Gets or sets whether the user is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the updated timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}
