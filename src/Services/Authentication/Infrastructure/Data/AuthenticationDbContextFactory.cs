using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Authentication.Infrastructure.Data;

/// <summary>
/// Factory for creating AuthenticationDbContext instances for migrations
/// </summary>
public class AuthenticationDbContextFactory : IDesignTimeDbContextFactory<AuthenticationDbContext>
{
    public AuthenticationDbContext CreateDbContext(string[] args)
    {
        var connectionString = "Host=localhost;Port=5432;Database=authentication_service_db;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AuthenticationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3);
            npgsqlOptions.CommandTimeout(30);
        });

        return new AuthenticationDbContext(optionsBuilder.Options);
    }
}
