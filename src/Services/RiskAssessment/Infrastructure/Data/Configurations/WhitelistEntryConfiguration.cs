using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiskAssessment.Domain.Models;

namespace RiskAssessment.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration for WhitelistEntry entity
/// </summary>
public sealed class WhitelistEntryConfiguration : IEntityTypeConfiguration<WhitelistEntry>
{
    public void Configure(EntityTypeBuilder<WhitelistEntry> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.EntityValue)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Reason)
            .HasColumnType("TEXT");

        builder.Property(e => e.AddedAt)
            .IsRequired();

        builder.Property(e => e.AddedBy)
            .HasMaxLength(100);

        // Unique constraint on entity_type + entity_value
        builder.HasIndex(e => new { e.EntityType, e.EntityValue })
            .IsUnique()
            .HasDatabaseName("uq_whitelist_entry");

        // Other indexes
        builder.HasIndex(e => e.EntityType)
            .HasDatabaseName("idx_whitelist_entity_type");

        builder.HasIndex(e => e.EntityValue)
            .HasDatabaseName("idx_whitelist_entity_value");
    }
}
