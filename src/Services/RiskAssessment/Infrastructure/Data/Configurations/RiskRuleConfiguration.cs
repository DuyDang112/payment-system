using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiskAssessment.Domain.Models;

namespace RiskAssessment.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration for RiskRule entity
/// </summary>
public sealed class RiskRuleConfiguration : IEntityTypeConfiguration<RiskRule>
{
    public void Configure(EntityTypeBuilder<RiskRule> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RuleId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.RuleName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Description)
            .HasColumnType("TEXT");

        builder.Property(e => e.RuleType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Version)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.Priority)
            .IsRequired();

        builder.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ScoreImpact)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.ConditionsJson)
            .IsRequired()
            .HasColumnType("JSONB");

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired(false);

        builder.Property(e => e.DeletedAt)
            .IsRequired(false);

        // Unique constraint on rule_id + version
        builder.HasIndex(e => new { e.RuleId, e.Version })
            .IsUnique()
            .HasDatabaseName("uq_rule_version");

        // Other indexes
        builder.HasIndex(e => e.RuleId)
            .HasDatabaseName("idx_rule_id");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("idx_is_active");

        builder.HasIndex(e => e.Priority)
            .HasDatabaseName("idx_priority");

        builder.HasIndex(e => e.RuleType)
            .HasDatabaseName("idx_rule_type");
    }
}
