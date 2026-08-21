using Authentication.Domain;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Infrastructure.Data;

/// <summary>
/// Simplified database context for Authentication Service
/// Contains only User, Role and related entities for JWT authentication
/// </summary>
public class AuthenticationDbContext : DbContext
{
    public AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options)
        : base(options)
    {
    }

    // Application tables
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Domain.UserClaim> UserClaims => Set<Domain.UserClaim>();
    public DbSet<Domain.RoleClaim> RoleClaims => Set<Domain.RoleClaim>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure User entity
        builder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(100).IsRequired();
            entity.Property(u => u.FirstName).HasMaxLength(50);
            entity.Property(u => u.LastName).HasMaxLength(50);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.IsActive).HasDefaultValue(true);
            entity.Property(u => u.EmailVerified).HasDefaultValue(false);
        });

        // Configure Role entity
        builder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.Name).IsUnique();

            entity.Property(r => r.Name).HasMaxLength(50).IsRequired();
            entity.Property(r => r.Description).HasMaxLength(200).IsRequired();
        });

        // Configure UserRole (many-to-many)
        builder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure UserClaim
        builder.Entity<Domain.UserClaim>(entity =>
        {
            entity.HasKey(uc => uc.Id);

            entity.HasOne(uc => uc.User)
                .WithMany(u => u.Claims)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RoleClaim
        builder.Entity<Domain.RoleClaim>(entity =>
        {
            entity.HasKey(rc => rc.Id);

            entity.HasOne(rc => rc.Role)
                .WithMany(r => r.Claims)
                .HasForeignKey(rc => rc.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}