using Microsoft.EntityFrameworkCore;
using PaymentProcessing.Domain.Models;
using PaymentProcessing.Infrastructure.Data.Configurations;

namespace PaymentProcessing.Infrastructure.Data;

/// <summary>
/// Database context for Payment Processing
/// </summary>
public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<StateTransition> StateTransitions => Set<StateTransition>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("payment_processing");

        // Apply configurations
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentAttemptConfiguration());
        modelBuilder.ApplyConfiguration(new StateTransitionConfiguration());
        modelBuilder.ApplyConfiguration(new IdempotencyKeyConfiguration());

        // Convert enums to strings with snake_case naming
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType.IsEnum)
                {
                    property.SetColumnName(ConvertToSnakeCase(property.Name));
                }
            }
        }
    }

    private static string ConvertToSnakeCase(string input)
    {
        return string.Concat(input.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToLower();
    }
}
