namespace RiskAssessment.Domain.Models;

/// <summary>
/// Blacklist entry for blocking entities
/// </summary>
public sealed class BlacklistEntry
{
    private BlacklistEntry()
    {
        // For EF Core
    }

    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public string EntityValue { get; private set; } = string.Empty;
    public string? Reason { get; private set; }
    public DateTime AddedAt { get; private set; }
    public string? AddedBy { get; private set; }

    /// <summary>
    /// Entity types that can be blacklisted
    /// </summary>
    public const string EntityTypeCustomer = "CUSTOMER";
    public const string EntityTypeIp = "IP";
    public const string EntityTypeEmail = "EMAIL";
    public const string EntityTypeCard = "CARD";

    /// <summary>
    /// Factory method to create a blacklist entry
    /// </summary>
    public static BlacklistEntry Create(
        string entityType,
        string entityValue,
        string? reason = null,
        string? addedBy = null)
    {
        var now = DateTime.UtcNow;

        return new BlacklistEntry
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityValue = entityValue,
            Reason = reason,
            AddedAt = now,
            AddedBy = addedBy
        };
    }

    /// <summary>
    /// Check if entity type is valid
    /// </summary>
    public static bool IsValidEntityType(string entityType)
    {
        return entityType switch
        {
            EntityTypeCustomer => true,
            EntityTypeIp => true,
            EntityTypeEmail => true,
            EntityTypeCard => true,
            _ => false
        };
    }
}
