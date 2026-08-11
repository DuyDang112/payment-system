using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BankAdapter.Infrastructure.Data;

/// <summary>
/// Design-time context factory for EF Core migrations
/// </summary>
public class BankAdapterDbContextFactory : IDesignTimeDbContextFactory<BankAdapterDbContext>
{
    public BankAdapterDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<BankAdapterDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               "Host=localhost;Port=5432;Database=bank_adapter_db;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3);
        });

        return new BankAdapterDbContext(optionsBuilder.Options);
    }
}
