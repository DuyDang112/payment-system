using Authentication.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Infrastructure.Data;

/// <summary>
/// Seed data for the Authentication Service database
/// </summary>
public static class SeedData
{
    public static async Task SeedAsync(AuthenticationDbContext dbContext)
    {
        // Ensure database is created
        await dbContext.Database.EnsureCreatedAsync();

        await SeedRolesAsync(dbContext);
        await SeedAdminUserAsync(dbContext);
        await SeedTestUsersAsync(dbContext);
    }

    private static async Task SeedRolesAsync(AuthenticationDbContext dbContext)
    {
        if (await dbContext.Roles.AnyAsync())
        {
            return; // Roles already seeded
        }

        var roles = new List<Role>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Admin",
                Description = "Full system administrator with all permissions",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Operator",
                Description = "Payment operations staff with limited access",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "User",
                Description = "Regular user with basic permissions",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Auditor",
                Description = "Read-only access for auditing purposes",
                CreatedAt = DateTime.UtcNow
            }
        };

        await dbContext.Roles.AddRangeAsync(roles);
        await dbContext.SaveChangesAsync();

        Console.WriteLine("Seeded roles: Admin, Operator, User, Auditor");
    }

    private static async Task SeedAdminUserAsync(AuthenticationDbContext dbContext)
    {
        if (await dbContext.Users.AnyAsync(u => u.Username == "admin"))
        {
            return; // Admin user already exists
        }

        var adminRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        var passwordHasher = new PasswordHasher<User>();

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@payment-system.com",
            FirstName = "System",
            LastName = "Administrator",
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin123!");

        // Add admin role
        if (adminRole != null)
        {
            adminUser.UserRoles.Add(new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                AssignedAt = DateTime.UtcNow
            });
        }

        // Add admin claims
        foreach (var claim in new[]
        {
            new UserClaim
            {
                UserId = adminUser.Id,
                ClaimType = "permission",
                ClaimValue = "full_access"
            },
            new UserClaim
            {
                UserId = adminUser.Id,
                ClaimType = "tier",
                ClaimValue = "admin"
            }
        })
        {
            adminUser.Claims.Add(claim);
        }

        await dbContext.Users.AddAsync(adminUser);
        await dbContext.SaveChangesAsync();

        Console.WriteLine("Seeded admin user: admin (password: Admin123!)");
    }

    private static async Task SeedTestUsersAsync(AuthenticationDbContext dbContext)
    {
        if (await dbContext.Users.CountAsync(u => u.Username.StartsWith("test")) > 0)
        {
            return; // Test users already seeded
        }

        var userRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        var operatorRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Operator");
        var passwordHasher = new PasswordHasher<User>();

        var testUsers = new List<User>();

        // Test regular user
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "testuser@example.com",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        testUser.PasswordHash = passwordHasher.HashPassword(testUser, "TestUser123!");

        if (userRole != null)
        {
            testUser.UserRoles.Add(new UserRole
            {
                UserId = testUser.Id,
                RoleId = userRole.Id,
                AssignedAt = DateTime.UtcNow
            });
        }

        testUsers.Add(testUser);

        // Test operator
        var testOperator = new User
        {
            Id = Guid.NewGuid(),
            Username = "testoperator",
            Email = "testoperator@payment-system.com",
            FirstName = "Test",
            LastName = "Operator",
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        testOperator.PasswordHash = passwordHasher.HashPassword(testOperator, "TestOperator123!");

        if (operatorRole != null)
        {
            testOperator.UserRoles.Add(new UserRole
            {
                UserId = testOperator.Id,
                RoleId = operatorRole.Id,
                AssignedAt = DateTime.UtcNow
            });
        }

        testOperator.Claims.Add(new UserClaim
        {
            UserId = testOperator.Id,
            ClaimType = "permission",
            ClaimValue = "payment_read,payment_write,risk_read"
        });

        testUsers.Add(testOperator);

        await dbContext.Users.AddRangeAsync(testUsers);
        await dbContext.SaveChangesAsync();

        Console.WriteLine("Seeded test users: testuser, testoperator");
    }
}
