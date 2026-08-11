using Microsoft.EntityFrameworkCore;
using RiskAssessment.Domain.Models;
using RiskAssessment.Infrastructure.Data.Configurations;

namespace RiskAssessment.Infrastructure.Data;

/// <summary>
/// Database context for Risk Assessment Service with snake_case naming convention
/// </summary>
public sealed class RiskAssessmentDbContext : DbContext
{
    public RiskAssessmentDbContext(DbContextOptions<RiskAssessmentDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<RiskEvaluation> RiskEvaluations => Set<RiskEvaluation>();
    public DbSet<RiskRule> RiskRules => Set<RiskRule>();
    public DbSet<MerchantRiskProfile> MerchantRiskProfiles => Set<MerchantRiskProfile>();
    public DbSet<BlacklistEntry> BlacklistEntries => Set<BlacklistEntry>();
    public DbSet<WhitelistEntry> WhitelistEntries => Set<WhitelistEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("risk_assessment");

        // Apply all configurations
        modelBuilder.ApplyConfiguration(new RiskEvaluationConfiguration());
        modelBuilder.ApplyConfiguration(new RiskRuleConfiguration());
        modelBuilder.ApplyConfiguration(new MerchantRiskProfileConfiguration());
        modelBuilder.ApplyConfiguration(new BlacklistEntryConfiguration());
        modelBuilder.ApplyConfiguration(new WhitelistEntryConfiguration());

        // Set snake_case naming convention
        new SnakeCaseNamingConvention().SetSnakeCaseNaming(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Enable detailed error logging in development
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }
    }
}

/// <summary>
/// Sets snake_case naming convention for all database objects
/// </summary>
public sealed class SnakeCaseNamingConvention
{
    public void SetSnakeCaseNaming(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Set table name to snake_case
            var tableName = ToSnakeCase(entity.GetTableName() ?? entity.ClrType.Name);
            entity.SetTableName(tableName);

            // Set schema to risk_assessment
            entity.SetSchema("risk_assessment");

            // Set column names to snake_case
            foreach (var property in entity.GetProperties())
            {
                var columnName = ToSnakeCase(property.GetColumnBaseName() ?? property.Name);
                property.SetColumnName(columnName);
            }

            // Set primary key names to snake_case
            foreach (var key in entity.GetKeys())
            {
                var keyName = ToSnakeCase(key.GetName() ?? key.Properties.First().Name + "_pk");
                key.SetName(keyName);
            }

            // Set foreign key names to snake_case
            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var fkName = ToSnakeCase(
                    foreignKey.GetConstraintName() ??
                    $"fk_{foreignKey.DeclaringEntityType.GetTableName()}_{foreignKey.PrincipalEntityType.GetTableName()}");
                foreignKey.SetConstraintName(fkName);
            }

            // Set index names to snake_case
            foreach (var index in entity.GetIndexes())
            {
                var indexName = ToSnakeCase(
                    index.GetDatabaseName() ??
                    $"idx_{entity.GetTableName()}_{string.Join("_", index.Properties.Select(p => p.Name))}");
                index.SetDatabaseName(indexName);
            }

            // Set unique constraint names to snake_case
            foreach (var unique in entity.GetIndexes().Where(i => i.IsUnique))
            {
                var uniqueName = ToSnakeCase(
                    unique.GetDatabaseName() ??
                    $"uq_{entity.GetTableName()}_{string.Join("_", unique.Properties.Select(p => p.Name))}");
                unique.SetDatabaseName(uniqueName);
            }
        }
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var span = new Span<char>(new char[input.Length * 2]);
        var position = 0;

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c) && i > 0 && !char.IsUpper(input[i - 1]))
            {
                span[position++] = '_';
            }

            span[position++] = char.ToLowerInvariant(c);
        }

        return span.Slice(0, position).ToString();
    }
}
