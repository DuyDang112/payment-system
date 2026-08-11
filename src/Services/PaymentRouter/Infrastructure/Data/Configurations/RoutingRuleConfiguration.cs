using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentRouter.Domain.Models;

namespace PaymentRouter.Infrastructure.Data.Configurations;

public class RoutingRuleConfiguration : IEntityTypeConfiguration<RoutingRule>
{
    public void Configure(EntityTypeBuilder<RoutingRule> builder)
    {
        builder.ToTable("routing_rules", "payment_router");

        builder.HasKey(x => x.RuleId);

        builder.Property(x => x.RuleId)
            .HasMaxLength(50)
            .ValueGeneratedNever();

        builder.Property(x => x.MerchantId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Priority)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.ConditionsJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.PreferredProviderIds)
            .IsRequired()
            .HasColumnType("text[]");

        builder.Property(x => x.Strategy)
            .IsRequired()
            .HasConversion(
                s => s.ToString(),
                s => (RoutingStrategy)Enum.Parse(typeof(RoutingStrategy), s));

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.MerchantId)
            .HasDatabaseName("idx_routing_rules_merchant_id");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("idx_routing_rules_is_active");

        builder.HasIndex(x => x.Priority)
            .HasDatabaseName("idx_routing_rules_priority");

        // Unique constraint
        builder.HasIndex(x => new { x.MerchantId, x.Name })
            .IsUnique()
            .HasDatabaseName("uq_routing_rules_merchant_name");
    }
}
