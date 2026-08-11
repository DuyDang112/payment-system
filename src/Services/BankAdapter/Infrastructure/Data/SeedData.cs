using BankAdapter.Domain;
using Microsoft.EntityFrameworkCore;

namespace BankAdapter.Infrastructure.Data;

/// <summary>
/// Seed data for Bank Adapter database
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Seeds the database with sample data
    /// </summary>
    public static async Task SeedAsync(BankAdapterDbContext dbContext)
    {
        // Ensure database is created
        await dbContext.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (await dbContext.Providers.AnyAsync())
        {
            return; // Database already seeded
        }

        var providers = await SeedProviders(dbContext);
        await SeedProviderRequestLogs(dbContext, providers);

        Console.WriteLine("✅ Bank Adapter database seeded successfully");
        Console.WriteLine($"✅ Seeded {providers.Count} payment provider integrations");
        Console.WriteLine($"✅ Seeded sample provider request logs");
        Console.WriteLine($"✅ Providers configured: Stripe, Adyen, Chase Bank, PayPal, Square");
    }

    private static async Task<List<Provider>> SeedProviders(BankAdapterDbContext dbContext)
    {
        var providers = new List<Provider>();

        // 1. Stripe - Popular payment gateway
        var stripe = Provider.Create(
            providerId: "stripe",
            providerType: ProviderType.Stripe,
            apiVersion: "v1",
            endpoint: "https://api.stripe.com/v1",
            authConfig: new AuthConfig(
                AuthType.ApiKey,
                new Dictionary<string, string>
                {
                    { "api_key", "sk_test_51ExampleKey" }
                }
            ),
            rateLimits: new RateLimits(
                MaxRequestsPerSecond: 100,
                MaxConcurrentRequests: 20
            ),
            retryConfig: new RetryConfig(
                MaxRetries: 3,
                BackoffMs: 1000,
                RetryableErrors: new HashSet<string> { "timeout", "rate_limit_exceeded", "transient_error" }
            ),
            timeouts: new Timeouts(
                ConnectTimeoutMs: 5000,
                ReadTimeoutMs: 30000
            ),
            supportedOperations: new HashSet<Operation>
            {
                Operation.Authorize,
                Operation.Capture,
                Operation.Refund,
                Operation.Void
            }
        );
        providers.Add(stripe);

        // 2. Adyen - Global payment platform
        var adyen = Provider.Create(
            providerId: "adyen",
            providerType: ProviderType.Adyen,
            apiVersion: "v71",
            endpoint: "https://pal-test.adyen.com/pal/servlet/Payment/v71",
            authConfig: new AuthConfig(
                AuthType.ApiKey,
                new Dictionary<string, string>
                {
                    { "api_key", "AQExhZhdjgYJkvB..." },
                    { "merchant_account", "YourMerchantAccount" }
                }
            ),
            rateLimits: new RateLimits(
                MaxRequestsPerSecond: 80,
                MaxConcurrentRequests: 15
            ),
            retryConfig: new RetryConfig(
                MaxRetries: 3,
                BackoffMs: 1500,
                RetryableErrors: new HashSet<string> { "timeout", "rate_limit_exceeded", "network_error" }
            ),
            timeouts: new Timeouts(
                ConnectTimeoutMs: 6000,
                ReadTimeoutMs: 35000
            ),
            supportedOperations: new HashSet<Operation>
            {
                Operation.Authorize,
                Operation.Capture,
                Operation.Refund,
                Operation.Void
            }
        );
        providers.Add(adyen);

        // 3. Chase Bank - Direct bank integration
        var chase = Provider.Create(
            providerId: "chase_bank",
            providerType: ProviderType.Bank,
            apiVersion: "v2",
            endpoint: "https://api.chase.com/payments",
            authConfig: new AuthConfig(
                AuthType.MutualTls,
                new Dictionary<string, string>
                {
                    { "client_id", "chase_client_id" },
                    { "client_secret", "chase_secret" },
                    { "certificate_path", "/certs/chase.crt" }
                }
            ),
            rateLimits: new RateLimits(
                MaxRequestsPerSecond: 50,
                MaxConcurrentRequests: 10
            ),
            retryConfig: new RetryConfig(
                MaxRetries: 2,
                BackoffMs: 2000,
                RetryableErrors: new HashSet<string> { "timeout", "service_unavailable" }
            ),
            timeouts: new Timeouts(
                ConnectTimeoutMs: 8000,
                ReadTimeoutMs: 45000
            ),
            supportedOperations: new HashSet<Operation>
            {
                Operation.Authorize,
                Operation.Capture,
                Operation.Refund,
                Operation.Void
            }
        );
        providers.Add(chase);

        // 4. PayPal - Digital wallet provider
        var paypal = Provider.Create(
            providerId: "paypal",
            providerType: ProviderType.Wallet,
            apiVersion: "v1",
            endpoint: "https://api-m.sandbox.paypal.com/v1",
            authConfig: new AuthConfig(
                AuthType.OAuth,
                new Dictionary<string, string>
                {
                    { "client_id", "paypal_client_id" },
                    { "client_secret", "paypal_secret" }
                }
            ),
            rateLimits: new RateLimits(
                MaxRequestsPerSecond: 100,
                MaxConcurrentRequests: 25
            ),
            retryConfig: new RetryConfig(
                MaxRetries: 3,
                BackoffMs: 1000,
                RetryableErrors: new HashSet<string> { "timeout", "rate_limit_exceeded", "transient_error" }
            ),
            timeouts: new Timeouts(
                ConnectTimeoutMs: 5000,
                ReadTimeoutMs: 30000
            ),
            supportedOperations: new HashSet<Operation>
            {
                Operation.Authorize,
                Operation.Capture,
                Operation.Refund,
                Operation.Void
            }
        );
        providers.Add(paypal);

        // 5. Square - Point of sale provider
        var square = Provider.Create(
            providerId: "square",
            providerType: ProviderType.Stripe,
            apiVersion: "v2",
            endpoint: "https://connect.squareupsandbox.com/v2",
            authConfig: new AuthConfig(
                AuthType.OAuth,
                new Dictionary<string, string>
                {
                    { "access_token", "sandbox-sq0atb-abc123" },
                    { "environment", "sandbox" }
                }
            ),
            rateLimits: new RateLimits(
                MaxRequestsPerSecond: 90,
                MaxConcurrentRequests: 18
            ),
            retryConfig: new RetryConfig(
                MaxRetries: 2,
                BackoffMs: 1500,
                RetryableErrors: new HashSet<string> { "timeout", "rate_limit_exceeded" }
            ),
            timeouts: new Timeouts(
                ConnectTimeoutMs: 6000,
                ReadTimeoutMs: 35000
            ),
            supportedOperations: new HashSet<Operation>
            {
                Operation.Authorize,
                Operation.Capture,
                Operation.Refund,
                Operation.Void
            }
        );
        providers.Add(square);

        // 6. Wise (formerly TransferWise) - International transfers
        var wise = Provider.Create(
            providerId: "wise",
            providerType: ProviderType.Bank,
            apiVersion: "v1",
            endpoint: "https://api.sandbox.transferwise.tech/v1",
            authConfig: new AuthConfig(
                AuthType.ApiKey,
                new Dictionary<string, string>
                {
                    { "api_token", "wise_api_token" }
                }
            ),
            rateLimits: new RateLimits(
                MaxRequestsPerSecond: 60,
                MaxConcurrentRequests: 12
            ),
            retryConfig: new RetryConfig(
                MaxRetries: 3,
                BackoffMs: 2000,
                RetryableErrors: new HashSet<string> { "timeout", "rate_limit_exceeded", "transient_error" }
            ),
            timeouts: new Timeouts(
                ConnectTimeoutMs: 7000,
                ReadTimeoutMs: 40000
            ),
            supportedOperations: new HashSet<Operation>
            {
                Operation.Authorize,
                Operation.Capture,
                Operation.Refund
            }
        );
        providers.Add(wise);

        // Add all providers to database
        await dbContext.Providers.AddRangeAsync(providers);
        await dbContext.SaveChangesAsync();

        return providers;
    }

    private static async Task SeedProviderRequestLogs(BankAdapterDbContext dbContext, List<Provider> providers)
    {
        var logs = new List<ProviderRequestLog>();
        var random = new Random();

        // Create sample request logs for different operations
        var operations = new[] { Operation.Authorize, Operation.Capture, Operation.Refund, Operation.Void };
        var paymentIds = new[]
        {
            "pay_test_001", "pay_test_002", "pay_test_003", "pay_test_004", "pay_test_005"
        };

        // Generate 20 sample request logs with different outcomes
        for (int i = 0; i < 20; i++)
        {
            var provider = providers[random.Next(providers.Count)];
            var paymentId = paymentIds[random.Next(paymentIds.Length)];
            var operation = operations[random.Next(operations.Length)];
            var isSuccess = random.Next(10) > 3; // 70% success rate

            var log = ProviderRequestLog.Create(
                paymentId,
                provider.ProviderId,
                operation,
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    amount = 100.50 + (random.NextDouble() * 1000),
                    currency = "USD",
                    card_token = $"tok_{Guid.NewGuid():N}"
                })
            );

            // Simulate request completion
            if (isSuccess)
            {
                var duration = random.Next(200, 2000);
                log.CompleteSuccess(
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        transaction_id = $"txn_{Guid.NewGuid():N}",
                        status = "succeeded",
                        amount = 100.50 + (random.NextDouble() * 1000),
                        currency = "USD"
                    }),
                    200
                );

                // Manually set duration to match the response
                log.GetType().GetProperty("Duration")?.SetValue(log, duration);
            }
            else
            {
                var isRetryable = random.Next(10) > 5;
                var duration = random.Next(1000, 5000);
                log.CompleteError(
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        error = new
                        {
                            type = isRetryable ? "transient_error" : "validation_error",
                            message = isRetryable ? "Temporary network error" : "Invalid card details"
                        }
                    }),
                    isRetryable ? 503 : 400,
                    isRetryable ? "TRANSIENT_ERROR" : "VALIDATION_ERROR",
                    isRetryable ? "Temporary network error, please retry" : "Card number is invalid",
                    isRetryable ? ProviderResult.RetryableError : ProviderResult.FatalError
                );

                // Set retry count if retryable
                if (isRetryable && random.Next(10) > 5)
                {
                    for (int r = 0; r < random.Next(1, 3); r++)
                    {
                        log.IncrementRetry();
                    }
                }
            }

            logs.Add(log);
        }

        await dbContext.ProviderRequestLogs.AddRangeAsync(logs);
        await dbContext.SaveChangesAsync();
    }
}
