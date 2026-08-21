using Duende.IdentityServer.Models;
using Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;
using AppIdentityUser = Identity.Domain.IdentityUser;

namespace Identity.Infrastructure.Data;

/// <summary>
/// Seed data for the Identity service
/// </summary>
public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var scope = serviceProvider.CreateScope();

        try
        {
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Check if any users exist - only if the database is properly initialized
            if (await context.Users.AnyAsync())
            {
                return; // Database has been seeded
            }

            // Create default admin user
            var adminUser = new AppIdentityUser
            {
                UserName = "admin@payment-system.com",
                Email = "admin@payment-system.com",
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppIdentityUser>>();
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                Console.WriteLine("Admin user created successfully");
            }
            else
            {
                Console.WriteLine($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during database seeding: {ex.Message}");
            // Don't throw - let the application start even if seeding fails
        }
    }

    public static IEnumerable<IdentityResource> GetIdentityResources()
    {
        return new List<IdentityResource>
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email()
        };
    }

    public static IEnumerable<ApiScope> GetApiScopes()
    {
        return new List<ApiScope>
        {
            new ApiScope("payment_api", "Payment API"),
            new ApiScope("risk_assessment_api", "Risk Assessment API"),
            new ApiScope("bank_adapter_api", "Bank Adapter API")
        };
    }

    public static IEnumerable<ApiResource> GetApiResources()
    {
        return new List<ApiResource>
        {
            new ApiResource("payment_api", "Payment API")
            {
                Scopes = { "payment_api" }
            },
            new ApiResource("risk_assessment_api", "Risk Assessment API")
            {
                Scopes = { "risk_assessment_api" }
            },
            new ApiResource("bank_adapter_api", "Bank Adapter API")
            {
                Scopes = { "bank_adapter_api" }
            }
        };
    }

    public static IEnumerable<Client> GetClients()
    {
        return new List<Client>
        {
            // Payment Web App - Authorization Code Flow
            new Client
            {
                ClientId = "payment-webapp",
                ClientName = "Payment Web Application",
                ClientSecrets = { new Secret("K7gNUu4qZBWf0qVvMY8z2Dj8Qx9kXFjG2vCHZNFj2E8".Sha256()) },
                AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                RequirePkce = true,
                RequireClientSecret = false,
                RedirectUris = { "https://localhost:5001/signin-oidc" },
                PostLogoutRedirectUris = { "https://localhost:5001/signout-callback-oidc" },
                AllowedScopes = { "openid", "profile", "email", "payment_api", "offline_access" },
                AllowOfflineAccess = true,
                AccessTokenLifetime = 3600 // 1 hour
            },

            // Payment Processing Service - Client Credentials Flow
            new Client
            {
                ClientId = "payment-processing-service",
                ClientName = "Payment Processing Service",
                ClientSecrets = { new Secret("S5gNUu4qZBWf0qVvMY8z2Dj8Qx9kXFjG2vCHZNFj2E5".Sha256()) },
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                RequireClientSecret = true,
                AllowedScopes = { "payment_api", "risk_assessment_api" },
                AccessTokenLifetime = 3600, // 1 hour
                Claims =
                {
                    new ClientClaim("service_type", "payment_processing"),
                    new ClientClaim("environment", "production")
                }
            },

            // Bank Adapter Service - Client Credentials Flow
            new Client
            {
                ClientId = "bank-adapter-service",
                ClientName = "Bank Adapter Service",
                ClientSecrets = { new Secret("L2gNUu4qZBWf0qVvMY8z2Dj8Qx9kXFjG2vCHZNFj2E3".Sha256()) },
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                RequireClientSecret = true,
                AllowedScopes = { "bank_adapter_api" },
                AccessTokenLifetime = 3600, // 1 hour
                Claims =
                {
                    new ClientClaim("service_type", "bank_adapter"),
                    new ClientClaim("environment", "production")
                }
            }
        };
    }
}

