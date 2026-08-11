using BankAdapter.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BankAdapter.Infrastructure.Data;

/// <summary>
/// Database context for Bank Adapter Service
/// </summary>
public sealed class BankAdapterDbContext : DbContext
{
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<ProviderRequestLog> ProviderRequestLogs => Set<ProviderRequestLog>();

    public BankAdapterDbContext(DbContextOptions<BankAdapterDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("bank_adapter");

        // Configure Provider entity
        modelBuilder.Entity<Provider>(entity =>
        {
            entity.ToTable("providers");

            entity.HasKey(e => e.IntegrationId);
            entity.HasIndex(e => e.ProviderId);

            entity.Property(e => e.IntegrationId)
                .HasColumnName("integration_id")
                .HasMaxLength(200);

            entity.Property(e => e.ProviderId)
                .HasColumnName("provider_id")
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.ProviderType)
                .HasColumnName("provider_type")
                .IsRequired();

            entity.Property(e => e.ApiVersion)
                .HasColumnName("api_version")
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Endpoint)
                .HasColumnName("endpoint")
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(e => e.AuthConfig)
                .HasColumnName("auth_config")
                .IsRequired()
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<AuthConfig>(v, (JsonSerializerOptions)null));

            entity.Property(e => e.RateLimits)
                .HasColumnName("rate_limits")
                .IsRequired()
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<RateLimits>(v, (JsonSerializerOptions)null));

            entity.Property(e => e.RetryConfig)
                .HasColumnName("retry_config")
                .IsRequired()
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<RetryConfig>(v, (JsonSerializerOptions)null));

            entity.Property(e => e.Timeouts)
                .HasColumnName("timeouts")
                .IsRequired()
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Timeouts>(v, (JsonSerializerOptions)null));

            entity.Property(e => e.SupportedOperations)
                .HasColumnName("supported_operations")
                .IsRequired()
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<HashSet<Operation>>(v, (JsonSerializerOptions)null));

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");
        });

        // Configure ProviderRequestLog entity
        modelBuilder.Entity<ProviderRequestLog>(entity =>
        {
            entity.ToTable("provider_request_logs");

            entity.HasKey(e => e.LogId);
            entity.HasIndex(e => e.PaymentId);
            entity.HasIndex(e => e.ProviderId);
            entity.HasIndex(e => e.Operation);
            entity.HasIndex(e => e.Result);
            entity.HasIndex(e => e.RequestSentAt);

            entity.Property(e => e.LogId)
                .HasColumnName("log_id")
                .HasMaxLength(200);

            entity.Property(e => e.PaymentId)
                .HasColumnName("payment_id")
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.ProviderId)
                .HasColumnName("provider_id")
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Operation)
                .HasColumnName("operation")
                .IsRequired();

            entity.Property(e => e.RequestSentAt)
                .HasColumnName("request_sent_at")
                .IsRequired();

            entity.Property(e => e.ResponseReceivedAt)
                .HasColumnName("response_received_at");

            entity.Property(e => e.Duration)
                .HasColumnName("duration");

            entity.Property(e => e.RequestPayload)
                .HasColumnName("request_payload")
                .IsRequired();

            entity.Property(e => e.ResponsePayload)
                .HasColumnName("response_payload")
                .IsRequired();

            entity.Property(e => e.HttpStatusCode)
                .HasColumnName("http_status_code");

            entity.Property(e => e.Result)
                .HasColumnName("result")
                .IsRequired();

            entity.Property(e => e.ErrorCode)
                .HasColumnName("error_code")
                .HasMaxLength(200);

            entity.Property(e => e.ErrorMessage)
                .HasColumnName("error_message")
                .HasMaxLength(2000);

            entity.Property(e => e.RetryCount)
                .HasColumnName("retry_count")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
        });
    }
}
