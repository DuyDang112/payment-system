using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentProcessing.Domain.Models;

namespace PaymentProcessing.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for IdempotencyKey entity
/// </summary>
public sealed class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("idempotency_keys");

        builder.HasKey(ik => ik.Id);
        builder.HasIndex(ik => new { ik.MerchantId, ik.Key }).IsUnique();
        builder.HasIndex(ik => ik.ExpiresAt);

        builder.Property(ik => ik.Key)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(ik => ik.MerchantId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(ik => ik.PaymentId)
            .IsRequired();

        builder.Property(ik => ik.CreatedAt)
            .IsRequired();

        builder.Property(ik => ik.ExpiresAt)
            .IsRequired();
    }
}
