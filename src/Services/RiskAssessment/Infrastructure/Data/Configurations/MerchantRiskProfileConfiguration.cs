using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiskAssessment.Domain.Models;

namespace RiskAssessment.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration for MerchantRiskProfile entity
/// </summary>
public sealed class MerchantRiskProfileConfiguration : IEntityTypeConfiguration<MerchantRiskProfile>
{
    public void Configure(EntityTypeBuilder<MerchantRiskProfile> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.MerchantId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(e => e.MerchantId)
            .IsUnique()
            .HasDatabaseName("uq_merchant_id");

        builder.Property(e => e.RiskLevel)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.AutoRejectThreshold)
            .IsRequired()
            .HasDefaultValue(80);

        builder.Property(e => e.ManualReviewThreshold)
            .IsRequired()
            .HasDefaultValue(50);

        builder.Property(e => e.MaxTransactionAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.MaxTransactionCurrency)
            .HasMaxLength(3);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Ignore collection navigation properties
        builder.Ignore(e => e.EnabledRuleIds);
        builder.Ignore(e => e.WhitelistedCountries);
        builder.Ignore(e => e.BlacklistedCountries);
        builder.Ignore(e => e.VelocityLimits);
    }
}
