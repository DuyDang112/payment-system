using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentRouter.Domain.Models;

namespace PaymentRouter.Infrastructure.Data.Configurations;

public class PaymentProviderConfiguration : IEntityTypeConfiguration<PaymentProvider>
{
    public void Configure(EntityTypeBuilder<PaymentProvider> builder)
    {
        builder.ToTable("payment_providers", "payment_router");

        builder.HasKey(x => x.ProviderId);

        builder.Property(x => x.ProviderId)
            .HasMaxLength(50)
            .ValueGeneratedNever();

        builder.Property(x => x.ProviderName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ProviderType)
            .IsRequired()
            .HasConversion(
                p => p.ToString(),
                p => (ProviderType)Enum.Parse(typeof(ProviderType), p));

        builder.Property(x => x.SupportedCurrencies)
            .IsRequired()
            .HasColumnType("text[]");

        builder.Property(x => x.SupportedMethods)
            .IsRequired()
            .HasConversion(
                p => p.Select(pm => pm.ToString()).ToArray(),
                p => p.Select(pm => (PaymentMethod)Enum.Parse(typeof(PaymentMethod), pm)).ToArray())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<PaymentMethod[]>(
                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                c => c != null ? c.Aggregate(0, (hash, pm) => HashCode.Combine(hash, pm.GetHashCode())) : 0,
                c => c != null ? c.ToArray() : Array.Empty<PaymentMethod>()));

        builder.Property(x => x.Priority)
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .IsRequired();

        builder.Property(x => x.MinAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.MaxAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.CostConfigJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.MetricsJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.HealthStatus)
            .IsRequired()
            .HasConversion(
                h => h.ToString(),
                h => (HealthStatus)Enum.Parse(typeof(HealthStatus), h));

        builder.Property(x => x.CircuitBreakerState)
            .IsRequired()
            .HasConversion(
                c => c.ToString(),
                c => (CircuitState)Enum.Parse(typeof(CircuitState), c));

        builder.Property(x => x.LastHealthCheck)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.IsEnabled)
            .HasDatabaseName("idx_payment_providers_is_enabled");

        builder.HasIndex(x => x.HealthStatus)
            .HasDatabaseName("idx_payment_providers_health_status");

        builder.HasIndex(x => x.Priority)
            .HasDatabaseName("idx_payment_providers_priority");
    }
}
