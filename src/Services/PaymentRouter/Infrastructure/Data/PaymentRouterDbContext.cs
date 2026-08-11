using Microsoft.EntityFrameworkCore;
using PaymentRouter.Domain.Models;
using PaymentRouter.Infrastructure.Data.Configurations;

namespace PaymentRouter.Infrastructure.Data;

public class PaymentRouterDbContext : DbContext
{
    public PaymentRouterDbContext(DbContextOptions<PaymentRouterDbContext> options)
        : base(options)
    {
    }

    public DbSet<PaymentProvider> PaymentProviders { get; set; }
    public DbSet<RoutingDecision> RoutingDecisions { get; set; }
    public DbSet<RoutingRule> RoutingRules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new PaymentProviderConfiguration());
        modelBuilder.ApplyConfiguration(new RoutingDecisionConfiguration());
        modelBuilder.ApplyConfiguration(new RoutingRuleConfiguration());
    }
}
