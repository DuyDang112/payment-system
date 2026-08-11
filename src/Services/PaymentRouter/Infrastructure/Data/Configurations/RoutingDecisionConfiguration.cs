using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentRouter.Domain.Models;

namespace PaymentRouter.Infrastructure.Data.Configurations;

public class RoutingDecisionConfiguration : IEntityTypeConfiguration<RoutingDecision>
{
    public void Configure(EntityTypeBuilder<RoutingDecision> builder)
    {
        builder.ToTable("routing_decisions", "payment_router");

        builder.HasKey(x => x.DecisionId);

        builder.Property(x => x.DecisionId)
            .HasMaxLength(50)
            .ValueGeneratedNever();

        builder.Property(x => x.PaymentId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.MerchantId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.SelectedProviderId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.AlternativeProviderIds)
            .IsRequired()
            .HasColumnType("text[]");

        builder.Property(x => x.Strategy)
            .IsRequired()
            .HasConversion(
                s => s.ToString(),
                s => (RoutingStrategy)Enum.Parse(typeof(RoutingStrategy), s));

        builder.Property(x => x.DecisionReason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.CostEstimateJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.DecisionMadeAt)
            .IsRequired();

        builder.Property(x => x.Amount)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.PaymentMethod)
            .IsRequired()
            .HasConversion(
                pm => pm.ToString(),
                pm => (PaymentMethod)Enum.Parse(typeof(PaymentMethod), pm));

        // Indexes
        builder.HasIndex(x => x.PaymentId)
            .HasDatabaseName("idx_routing_decisions_payment_id");

        builder.HasIndex(x => x.MerchantId)
            .HasDatabaseName("idx_routing_decisions_merchant_id");

        builder.HasIndex(x => x.SelectedProviderId)
            .HasDatabaseName("idx_routing_decisions_selected_provider");

        builder.HasIndex(x => x.DecisionMadeAt)
            .HasDatabaseName("idx_routing_decisions_decision_made_at");
    }
}
