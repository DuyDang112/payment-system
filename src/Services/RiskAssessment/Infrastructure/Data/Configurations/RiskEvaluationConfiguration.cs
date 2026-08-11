using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiskAssessment.Domain.Models;

namespace RiskAssessment.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration for RiskEvaluation entity
/// </summary>
public sealed class RiskEvaluationConfiguration : IEntityTypeConfiguration<RiskEvaluation>
{
    public void Configure(EntityTypeBuilder<RiskEvaluation> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EvaluationId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(e => e.EvaluationId)
            .IsUnique();

        builder.Property(e => e.PaymentId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.MerchantId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.CustomerId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.RiskScore)
            .IsRequired();

        builder.Property(e => e.Decision)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.RuleVersion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.EvaluatedAt)
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.Currency)
            .HasMaxLength(3);

        builder.Property(e => e.CountryCode)
            .HasMaxLength(2);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45);

        builder.Property(e => e.CustomerEmail)
            .HasMaxLength(255);

        // Indexes
        builder.HasIndex(e => e.PaymentId)
            .HasDatabaseName("idx_payment_id");

        builder.HasIndex(e => e.MerchantId)
            .HasDatabaseName("idx_merchant_id");

        builder.HasIndex(e => e.CustomerId)
            .HasDatabaseName("idx_customer_id");

        builder.HasIndex(e => e.EvaluatedAt)
            .HasDatabaseName("idx_evaluated_at");

        builder.HasIndex(e => e.Decision)
            .HasDatabaseName("idx_risk_decision");

        // Ignore navigation properties
        builder.Ignore(e => e.TriggeredRules);
        builder.Ignore(e => e.DomainEvents);
    }
}
