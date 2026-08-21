using Identity.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Data;

/// <summary>
/// Database context for the Identity service
/// </summary>
public class IdentityDbContext : IdentityDbContext<IdentityUser, IdentityRole, Guid>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure IdentityUser
        builder.Entity<IdentityUser>(b =>
        {
            b.Property(u => u.FirstName).HasMaxLength(100);
            b.Property(u => u.LastName).HasMaxLength(100);
            b.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.HasIndex(u => u.CreatedAt);
            b.HasIndex(u => u.LastLoginAt);
        });

        // Configure IdentityRole
        builder.Entity<IdentityRole>(b =>
        {
            b.Property(r => r.Description).HasMaxLength(500);
            b.Property(r => r.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
