using Microsoft.EntityFrameworkCore;
using PaymentRouter.Domain.Models;

namespace PaymentRouter.Infrastructure.Data;

/// <summary>
/// Seed data for Payment Router database
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Seeds the database with sample data
    /// </summary>
    public static async Task SeedAsync(PaymentRouterDbContext dbContext)
    {
        // Ensure database is created
        await dbContext.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (await dbContext.PaymentProviders.AnyAsync())
        {
            return; // Database already seeded
        }

        var providers = await SeedPaymentProviders(dbContext);
        await SeedRoutingRules(dbContext, providers);
        await SeedRoutingDecisions(dbContext, providers);

        Console.WriteLine("✅ Payment Router database seeded successfully");
        Console.WriteLine($"✅ Seeded {providers.Count} payment providers");
        Console.WriteLine($"✅ Seeded routing rules for different strategies");
        Console.WriteLine($"✅ Seeded sample routing decisions");
    }

    private static async Task<List<PaymentProvider>> SeedPaymentProviders(PaymentRouterDbContext dbContext)
    {
        var providers = new List<PaymentProvider>();

        // 1. Stripe - Premium gateway with high success rate
        var stripe = PaymentProvider.Create(
            providerId: "stripe",
            providerName: "Stripe",
            providerType: ProviderType.PAYMENT_GATEWAY,
            supportedCurrencies: new[] { "USD", "EUR", "GBP", "CAD", "AUD", "JPY" },
            supportedMethods: new[] { PaymentMethod.CREDIT_CARD, PaymentMethod.DEBIT_CARD, PaymentMethod.DIGITAL_WALLET },
            priority: 1,
            costConfig: new CostConfiguration(
                new Money(0.30m, "USD"),
                2.9m,
                new Money(0.25m, "USD"),
                new Money(10.00m, "USD")
            )
        );
        stripe.UpdatePerformanceMetrics(new PerformanceMetrics(
            successRate: 0.995,
            p50Latency: 150,
            p95Latency: 300,
            p99Latency: 600,
            dailyVolume: 50000,
            failureRate: 0.005
        ));
        providers.Add(stripe);

        // 2. Adyen - Global payment gateway
        var adyen = PaymentProvider.Create(
            providerId: "adyen",
            providerName: "Adyen",
            providerType: ProviderType.PAYMENT_GATEWAY,
            supportedCurrencies: new[] { "USD", "EUR", "GBP", "CAD", "AUD", "JPY", "SGD", "HKD" },
            supportedMethods: new[] { PaymentMethod.CREDIT_CARD, PaymentMethod.DEBIT_CARD, PaymentMethod.DIGITAL_WALLET, PaymentMethod.BANK_TRANSFER },
            priority: 2,
            costConfig: new CostConfiguration(
                new Money(0.35m, "USD"),
                2.5m,
                new Money(0.20m, "USD"),
                new Money(12.00m, "USD")
            )
        );
        adyen.UpdatePerformanceMetrics(new PerformanceMetrics(
            successRate: 0.992,
            p50Latency: 180,
            p95Latency: 350,
            p99Latency: 700,
            dailyVolume: 75000,
            failureRate: 0.008
        ));
        providers.Add(adyen);

        // 3. Braintree - PayPal subsidiary
        var braintree = PaymentProvider.Create(
            providerId: "braintree",
            providerName: "Braintree",
            providerType: ProviderType.PAYMENT_GATEWAY,
            supportedCurrencies: new[] { "USD", "EUR", "GBP", "CAD", "AUD" },
            supportedMethods: new[] { PaymentMethod.CREDIT_CARD, PaymentMethod.DEBIT_CARD, PaymentMethod.DIGITAL_WALLET, PaymentMethod.BNPL },
            priority: 3,
            costConfig: new CostConfiguration(
                new Money(0.25m, "USD"),
                2.59m,
                new Money(0.18m, "USD"),
                new Money(8.00m, "USD")
            )
        );
        braintree.UpdatePerformanceMetrics(new PerformanceMetrics(
            successRate: 0.988,
            p50Latency: 200,
            p95Latency: 400,
            p99Latency: 800,
            dailyVolume: 35000,
            failureRate: 0.012
        ));
        providers.Add(braintree);

        // 4. Chase Bank - Direct bank integration
        var chase = PaymentProvider.Create(
            providerId: "chase_bank",
            providerName: "Chase Bank",
            providerType: ProviderType.BANK,
            supportedCurrencies: new[] { "USD" },
            supportedMethods: new[] { PaymentMethod.BANK_TRANSFER, PaymentMethod.CREDIT_CARD, PaymentMethod.DEBIT_CARD },
            priority: 4,
            costConfig: new CostConfiguration(
                new Money(0.50m, "USD"),
                1.5m,
                new Money(0.40m, "USD"),
                new Money(15.00m, "USD")
            )
        );
        chase.UpdatePerformanceMetrics(new PerformanceMetrics(
            successRate: 0.998,
            p50Latency: 250,
            p95Latency: 500,
            p99Latency: 1000,
            dailyVolume: 100000,
            failureRate: 0.002
        ));
        providers.Add(chase);

        // 5. PayPal - Digital wallet
        var paypal = PaymentProvider.Create(
            providerId: "paypal",
            providerName: "PayPal",
            providerType: ProviderType.WALLET,
            supportedCurrencies: new[] { "USD", "EUR", "GBP", "CAD", "AUD", "JPY" },
            supportedMethods: new[] { PaymentMethod.DIGITAL_WALLET },
            priority: 5,
            costConfig: new CostConfiguration(
                new Money(0.30m, "USD"),
                3.49m,
                new Money(0.25m, "USD"),
                new Money(15.00m, "USD")
            )
        );
        paypal.UpdatePerformanceMetrics(new PerformanceMetrics(
            successRate: 0.990,
            p50Latency: 220,
            p95Latency: 450,
            p99Latency: 900,
            dailyVolume: 60000,
            failureRate: 0.010
        ));
        providers.Add(paypal);

        // 6. Square - Point of sale specialist
        var square = PaymentProvider.Create(
            providerId: "square",
            providerName: "Square",
            providerType: ProviderType.PAYMENT_GATEWAY,
            supportedCurrencies: new[] { "USD", "CAD", "GBP", "AUD", "JPY" },
            supportedMethods: new[] { PaymentMethod.CREDIT_CARD, PaymentMethod.DEBIT_CARD, PaymentMethod.DIGITAL_WALLET },
            priority: 6,
            costConfig: new CostConfiguration(
                new Money(0.10m, "USD"),
                2.6m,
                new Money(0.08m, "USD"),
                new Money(7.50m, "USD")
            )
        );
        square.UpdatePerformanceMetrics(new PerformanceMetrics(
            successRate: 0.985,
            p50Latency: 190,
            p95Latency: 380,
            p99Latency: 750,
            dailyVolume: 40000,
            failureRate: 0.015
        ));
        providers.Add(square);

        // 7. Wise (formerly TransferWise) - International transfers
        var wise = PaymentProvider.Create(
            providerId: "wise",
            providerName: "Wise",
            providerType: ProviderType.BANK,
            supportedCurrencies: new[] { "USD", "EUR", "GBP", "CAD", "AUD", "JPY", "SGD", "INR" },
            supportedMethods: new[] { PaymentMethod.BANK_TRANSFER },
            priority: 7,
            costConfig: new CostConfiguration(
                new Money(0.50m, "USD"),
                1.2m,
                new Money(0.35m, "USD"),
                new Money(20.00m, "USD")
            )
        );
        wise.UpdatePerformanceMetrics(new PerformanceMetrics(
            successRate: 0.980,
            p50Latency: 500,
            p95Latency: 1200,
            p99Latency: 2400,
            dailyVolume: 15000,
            failureRate: 0.020
        ));
        providers.Add(wise);

        // 8. Klarna - BNPL specialist
        var klarna = PaymentProvider.Create(
            providerId: "klarna",
            providerName: "Klarna",
            providerType: ProviderType.PAYMENT_GATEWAY,
            supportedCurrencies: new[] { "USD", "EUR", "GBP", "CAD", "AUD" },
            supportedMethods: new[] { PaymentMethod.BNPL, PaymentMethod.CREDIT_CARD, PaymentMethod.DEBIT_CARD },
            priority: 8,
            costConfig: new CostConfiguration(
                new Money(0.30m, "USD"),
                4.0m,
                new Money(0.25m, "USD"),
                new Money(18.00m, "USD")
            )
        );
        klarna.UpdatePerformanceMetrics(new PerformanceMetrics(
            successRate: 0.975,
            p50Latency: 250,
            p95Latency: 500,
            p99Latency: 1000,
            dailyVolume: 12000,
            failureRate: 0.025
        ));
        providers.Add(klarna);

        // 9. BitPay - Crypto payments
        var bitpay = PaymentProvider.Create(
            providerId: "bitpay",
            providerName: "BitPay",
            providerType: ProviderType.WALLET,
            supportedCurrencies: new[] { "USD", "EUR", "GBP", "BTC", "ETH" },
            supportedMethods: new[] { PaymentMethod.CRYPTO, PaymentMethod.DIGITAL_WALLET },
            priority: 9,
            costConfig: new CostConfiguration(
                new Money(0.00m, "USD"),
                1.0m,
                new Money(0.00m, "USD"),
                new Money(5.00m, "USD")
            )
        );
        bitpay.UpdatePerformanceMetrics(new PerformanceMetrics(
            successRate: 0.970,
            p50Latency: 300,
            p95Latency: 600,
            p99Latency: 1200,
            dailyVolume: 5000,
            failureRate: 0.030
        ));
        providers.Add(bitpay);

        // 10. Worldpay - Enterprise gateway
        var worldpay = PaymentProvider.Create(
            providerId: "worldpay",
            providerName: "Worldpay",
            providerType: ProviderType.PAYMENT_GATEWAY,
            supportedCurrencies: new[] { "USD", "EUR", "GBP", "CAD", "AUD", "JPY", "SGD" },
            supportedMethods: new[] { PaymentMethod.CREDIT_CARD, PaymentMethod.DEBIT_CARD, PaymentMethod.BANK_TRANSFER },
            priority: 10,
            costConfig: new CostConfiguration(
                new Money(0.40m, "USD"),
                2.8m,
                new Money(0.30m, "USD"),
                new Money(14.00m, "USD")
            )
        );
        worldpay.UpdatePerformanceMetrics(new PerformanceMetrics(
            successRate: 0.996,
            p50Latency: 220,
            p95Latency: 440,
            p99Latency: 880,
            dailyVolume: 80000,
            failureRate: 0.004
        ));
        providers.Add(worldpay);

        // Set different health statuses for variety
        stripe.Disable(); // Simulate maintenance
        bitpay.UpdateHealthStatus(HealthStatus.DEGRADED);
        klarna.SetCircuitBreakerState(CircuitState.HALF_OPEN);

        await dbContext.PaymentProviders.AddRangeAsync(providers);
        await dbContext.SaveChangesAsync();

        return providers;
    }

    private static async Task SeedRoutingRules(PaymentRouterDbContext dbContext, List<PaymentProvider> providers)
    {
        var rules = new List<RoutingRule>();

        // 1. Default rule - Cost based routing
        var defaultRule = RoutingRule.Create(
            merchantId: "merchant_001",
            name: "Default Cost-Based Routing",
            priority: 100,
            conditions: new RoutingRuleCondition
            {
                Currencies = new[] { "USD", "EUR", "GBP" },
                PaymentMethods = new[] { PaymentMethod.CREDIT_CARD, PaymentMethod.DEBIT_CARD },
                MinAmount = 0.01m,
                MaxAmount = 100000m
            },
            preferredProviderIds: new[] { "square", "worldpay", "adyen" },
            strategy: RoutingStrategy.COST_BASED
        );
        rules.Add(defaultRule);

        // 2. High-value payments - Priority routing
        var highValueRule = RoutingRule.Create(
            merchantId: "merchant_001",
            name: "High-Value Priority Routing",
            priority: 90,
            conditions: new RoutingRuleCondition
            {
                Currencies = new[] { "USD", "EUR", "GBP" },
                MinAmount = 10000m,
                MaxAmount = null
            },
            preferredProviderIds: new[] { "chase_bank", "worldpay", "adyen" },
            strategy: RoutingStrategy.PRIORITY
        );
        rules.Add(highValueRule);

        // 3. International payments - Performance routing
        var internationalRule = RoutingRule.Create(
            merchantId: "merchant_001",
            name: "International Performance Routing",
            priority: 80,
            conditions: new RoutingRuleCondition
            {
                Currencies = new[] { "JPY", "SGD", "HKD" },
                PaymentMethods = new[] { PaymentMethod.CREDIT_CARD, PaymentMethod.DEBIT_CARD }
            },
            preferredProviderIds: new[] { "adyen", "worldpay", "wise" },
            strategy: RoutingStrategy.PERFORMANCE
        );
        rules.Add(internationalRule);

        // 4. Digital wallet routing - Round-robin
        var walletRule = RoutingRule.Create(
            merchantId: "merchant_001",
            name: "Digital Wallet Round-Robin",
            priority: 70,
            conditions: new RoutingRuleCondition
            {
                Currencies = new[] { "USD", "EUR", "GBP" },
                PaymentMethods = new[] { PaymentMethod.DIGITAL_WALLET }
            },
            preferredProviderIds: new[] { "paypal", "square", "braintree" },
            strategy: RoutingStrategy.ROUND_ROBIN
        );
        rules.Add(walletRule);

        // 5. BNPL specialist routing
        var bnplRule = RoutingRule.Create(
            merchantId: "merchant_001",
            name: "BNPL Specialist Routing",
            priority: 60,
            conditions: new RoutingRuleCondition
            {
                Currencies = new[] { "USD", "EUR", "GBP", "CAD", "AUD" },
                PaymentMethods = new[] { PaymentMethod.BNPL },
                MinAmount = 35m,
                MaxAmount = 1000m
            },
            preferredProviderIds: new[] { "klarna", "braintree" },
            strategy: RoutingStrategy.PRIORITY
        );
        rules.Add(bnplRule);

        // 6. Low-value fast payments
        var lowValueRule = RoutingRule.Create(
            merchantId: "merchant_001",
            name: "Low-Value Cost Optimized",
            priority: 50,
            conditions: new RoutingRuleCondition
            {
                Currencies = new[] { "USD", "EUR", "GBP" },
                PaymentMethods = new[] { PaymentMethod.CREDIT_CARD, PaymentMethod.DEBIT_CARD },
                MinAmount = 0.01m,
                MaxAmount = 50m
            },
            preferredProviderIds: new[] { "square", "stripe", "paypal" },
            strategy: RoutingStrategy.COST_BASED
        );
        rules.Add(lowValueRule);

        // 7. Crypto payments
        var cryptoRule = RoutingRule.Create(
            merchantId: "merchant_001",
            name: "Crypto Payment Routing",
            priority: 40,
            conditions: new RoutingRuleCondition
            {
                Currencies = new[] { "USD", "EUR", "GBP" },
                PaymentMethods = new[] { PaymentMethod.CRYPTO }
            },
            preferredProviderIds: new[] { "bitpay" },
            strategy: RoutingStrategy.PRIORITY
        );
        rules.Add(cryptoRule);

        // 8. Bank transfer routing
        var bankTransferRule = RoutingRule.Create(
            merchantId: "merchant_001",
            name: "Bank Transfer Routing",
            priority: 30,
            conditions: new RoutingRuleCondition
            {
                Currencies = new[] { "USD", "EUR", "GBP", "CAD", "AUD", "SGD" },
                PaymentMethods = new[] { PaymentMethod.BANK_TRANSFER },
                MinAmount = 1000m
            },
            preferredProviderIds: new[] { "wise", "chase_bank", "adyen" },
            strategy: RoutingStrategy.PERFORMANCE
        );
        rules.Add(bankTransferRule);

        await dbContext.RoutingRules.AddRangeAsync(rules);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedRoutingDecisions(PaymentRouterDbContext dbContext, List<PaymentProvider> providers)
    {
        var decisions = new List<RoutingDecision>();

        // 1. Standard credit card payment - Stripe (cost-based)
        decisions.Add(RoutingDecision.Create(
            paymentId: "pay_001",
            merchantId: "merchant_001",
            selectedProviderId: "square",
            alternativeProviderIds: new[] { "stripe", "worldpay" },
            strategy: RoutingStrategy.COST_BASED,
            decisionReason: "Lowest cost provider for USD credit card payment",
            costEstimate: new Money(0.55m, "USD"),
            amount: new Money(25.00m, "USD"),
            paymentMethod: PaymentMethod.CREDIT_CARD
        ));

        // 2. High-value payment - Chase Bank (priority)
        decisions.Add(RoutingDecision.Create(
            paymentId: "pay_002",
            merchantId: "merchant_001",
            selectedProviderId: "chase_bank",
            alternativeProviderIds: new[] { "worldpay", "adyen" },
            strategy: RoutingStrategy.PRIORITY,
            decisionReason: "High-value payment routed to highest priority provider",
            costEstimate: new Money(8.50m, "USD"),
            amount: new Money(25000.00m, "USD"),
            paymentMethod: PaymentMethod.BANK_TRANSFER
        ));

        // 3. Digital wallet payment - PayPal
        decisions.Add(RoutingDecision.Create(
            paymentId: "pay_003",
            merchantId: "merchant_001",
            selectedProviderId: "paypal",
            alternativeProviderIds: new[] { "square", "braintree" },
            strategy: RoutingStrategy.ROUND_ROBIN,
            decisionReason: "Round-robin selection for digital wallet payment",
            costEstimate: new Money(1.17m, "USD"),
            amount: new Money(25.00m, "USD"),
            paymentMethod: PaymentMethod.DIGITAL_WALLET
        ));

        // 4. International payment - Adyen
        decisions.Add(RoutingDecision.Create(
            paymentId: "pay_004",
            merchantId: "merchant_001",
            selectedProviderId: "adyen",
            alternativeProviderIds: new[] { "worldpay", "wise" },
            strategy: RoutingStrategy.PERFORMANCE,
            decisionReason: "Best performance provider for JPY payment",
            costEstimate: new Money(65.35m, "JPY"),
            amount: new Money(150000m, "JPY"),
            paymentMethod: PaymentMethod.CREDIT_CARD
        ));

        // 5. BNPL payment - Klarna
        decisions.Add(RoutingDecision.Create(
            paymentId: "pay_005",
            merchantId: "merchant_001",
            selectedProviderId: "klarna",
            alternativeProviderIds: new[] { "braintree" },
            strategy: RoutingStrategy.PRIORITY,
            decisionReason: "BNPL specialist provider selected",
            costEstimate: new Money(2.30m, "USD"),
            amount: new Money(50.00m, "USD"),
            paymentMethod: PaymentMethod.BNPL
        ));

        // 6. Crypto payment - BitPay
        decisions.Add(RoutingDecision.Create(
            paymentId: "pay_006",
            merchantId: "merchant_001",
            selectedProviderId: "bitpay",
            alternativeProviderIds: Array.Empty<string>(),
            strategy: RoutingStrategy.PRIORITY,
            decisionReason: "Only available crypto payment provider",
            costEstimate: new Money(0.50m, "USD"),
            amount: new Money(50.00m, "USD"),
            paymentMethod: PaymentMethod.CRYPTO
        ));

        // 7. Low-value payment - Square
        decisions.Add(RoutingDecision.Create(
            paymentId: "pay_007",
            merchantId: "merchant_001",
            selectedProviderId: "square",
            alternativeProviderIds: new[] { "paypal" },
            strategy: RoutingStrategy.COST_BASED,
            decisionReason: "Lowest cost provider for small amount",
            costEstimate: new Money(0.17m, "USD"),
            amount: new Money(2.99m, "USD"),
            paymentMethod: PaymentMethod.CREDIT_CARD
        ));

        // 8. Bank transfer - Wise
        decisions.Add(RoutingDecision.Create(
            paymentId: "pay_008",
            merchantId: "merchant_001",
            selectedProviderId: "wise",
            alternativeProviderIds: new[] { "chase_bank", "adyen" },
            strategy: RoutingStrategy.PERFORMANCE,
            decisionReason: "Best performance for international bank transfer",
            costEstimate: new Money(28.90m, "EUR"),
            amount: new Money(2500.00m, "EUR"),
            paymentMethod: PaymentMethod.BANK_TRANSFER
        ));

        // 9. Degraded provider fallback
        decisions.Add(RoutingDecision.Create(
            paymentId: "pay_009",
            merchantId: "merchant_001",
            selectedProviderId: "worldpay",
            alternativeProviderIds: new[] { "adyen", "braintree" },
            strategy: RoutingStrategy.PERFORMANCE,
            decisionReason: "Primary provider degraded, using backup with high success rate",
            costEstimate: new Money(0.96m, "USD"),
            amount: new Money(20.00m, "USD"),
            paymentMethod: PaymentMethod.DEBIT_CARD
        ));

        // 10. Round-robin selection
        decisions.Add(RoutingDecision.Create(
            paymentId: "pay_010",
            merchantId: "merchant_001",
            selectedProviderId: "braintree",
            alternativeProviderIds: new[] { "paypal", "square" },
            strategy: RoutingStrategy.ROUND_ROBIN,
            decisionReason: "Round-robin distribution for wallet payment",
            costEstimate: new Money(0.90m, "USD"),
            amount: new Money(25.00m, "USD"),
            paymentMethod: PaymentMethod.DIGITAL_WALLET
        ));

        await dbContext.RoutingDecisions.AddRangeAsync(decisions);
        await dbContext.SaveChangesAsync();
    }
}
