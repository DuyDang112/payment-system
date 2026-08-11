using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentProcessing.Domain.Models;

namespace PaymentProcessing.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for PaymentAttempt entity
/// </summary>
public sealed class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
    {
        builder.ToTable("payment_attempts");

        builder.HasKey(pa => pa.Id);
        builder.HasIndex(pa => pa.PaymentId);

        builder.Property(pa => pa.PaymentId)
            .IsRequired();

        builder.Property(pa => pa.AttemptNumber)
            .IsRequired();

        builder.Property(pa => pa.Provider)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(pa => pa.Status)
            .IsRequired();

        builder.Property(pa => pa.StartedAt)
            .IsRequired();

        builder.Property(pa => pa.CompletedAt);

        builder.Property(pa => pa.FailureReason)
            .HasMaxLength(500);
    }
}
