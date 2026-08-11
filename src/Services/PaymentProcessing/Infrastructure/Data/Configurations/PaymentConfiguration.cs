using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentProcessing.Domain.Models;

namespace PaymentProcessing.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Payment entity
/// </summary>
public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.PaymentId).IsUnique();
        builder.HasIndex(p => p.MerchantId);
        builder.HasIndex(p => p.IdempotencyKey);

        builder.Property(p => p.PaymentId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.MerchantId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.CustomerId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired();

        builder.Property(p => p.IdempotencyKey)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.PaymentMethodToken)
            .HasMaxLength(255);

        builder.Property(p => p.FailureReason)
            .HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        builder.Property(p => p.CompletedAt);

        builder.Property(p => p.RetryCount)
            .HasDefaultValue(0);

        // Configure navigation properties
        builder.HasMany(p => p.Attempts)
            .WithOne()
            .HasForeignKey(pa => pa.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.StateTransitions)
            .WithOne()
            .HasForeignKey(st => st.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore DomainEvents as it's not persisted to database
        builder.Ignore(p => p.DomainEvents);
    }
}
