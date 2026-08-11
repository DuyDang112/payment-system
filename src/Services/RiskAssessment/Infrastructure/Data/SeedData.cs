using Microsoft.EntityFrameworkCore;
using RiskAssessment.Domain.Models;
using System.Text.Json;

namespace RiskAssessment.Infrastructure.Data;

/// <summary>
/// Seed data for Risk Assessment database
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Seeds the database with sample data
    /// </summary>
    public static async Task SeedAsync(RiskAssessmentDbContext dbContext)
    {
        // Ensure database is created
        await dbContext.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (await dbContext.RiskRules.AnyAsync())
        {
            return; // Database already seeded
        }

        var rules = await SeedRiskRules(dbContext);
        await SeedMerchantRiskProfiles(dbContext);
        await SeedBlacklistEntries(dbContext);
        await SeedWhitelistEntries(dbContext);

        Console.WriteLine("✅ Risk Assessment database seeded successfully");
        Console.WriteLine($"✅ Seeded {rules.Count} risk rules");
        Console.WriteLine($"✅ Seeded merchant risk profiles with thresholds");
        Console.WriteLine($"✅ Seeded blacklist and whitelist entries");
    }

    private static async Task<List<RiskRule>> SeedRiskRules(RiskAssessmentDbContext dbContext)
    {
        var rules = new List<RiskRule>();

        // 1. High Transaction Amount Rule
        var highAmountRule = RiskRule.Create(
            ruleName: "High Transaction Amount",
            description: "Flag transactions above $10,000 as high risk",
            ruleType: RuleType.AMOUNT,
            priority: 1,
            action: RuleAction.ADD_SCORE,
            scoreImpact: 30,
            conditionsJson: JsonSerializer.Serialize(new
            {
                minAmount = 10000,
                currencies = new[] { "USD", "EUR", "GBP" }
            })
        );
        rules.Add(highAmountRule);

        // 2. Very High Transaction Amount Rule
        var veryHighAmountRule = RiskRule.Create(
            ruleName: "Very High Transaction Amount",
            description: "Flag transactions above $50,000 as very high risk",
            ruleType: RuleType.AMOUNT,
            priority: 1,
            action: RuleAction.ADD_SCORE,
            scoreImpact: 50,
            conditionsJson: JsonSerializer.Serialize(new
            {
                minAmount = 50000,
                currencies = new[] { "USD", "EUR", "GBP" }
            })
        );
        rules.Add(veryHighAmountRule);

        // 3. High Risk Country Rule
        var highRiskCountryRule = RiskRule.Create(
            ruleName: "High Risk Geographic Location",
            description: "Flag transactions from high-risk countries",
            ruleType: RuleType.GEO,
            priority: 2,
            action: RuleAction.ADD_SCORE,
            scoreImpact: 40,
            conditionsJson: JsonSerializer.Serialize(new
            {
                highRiskCountries = new[] { "AF", "KP", "IR", "MM", "SD", "SY", "YE" }
            })
        );
        rules.Add(highRiskCountryRule);

        // 4. Velocity Rule - Multiple Transactions
        var velocityRule = RiskRule.Create(
            ruleName: "High Transaction Velocity",
            description: "Flag customers with more than 5 transactions in 1 hour",
            ruleType: RuleType.VELOCITY,
            priority: 3,
            action: RuleAction.ADD_SCORE,
            scoreImpact: 25,
            conditionsJson: JsonSerializer.Serialize(new
            {
                maxTransactions = 5,
                windowMinutes = 60,
                windowType = "HOUR"
            })
        );
        rules.Add(velocityRule);

        // 5. IP Address Blacklist Rule
        var ipBlacklistRule = RiskRule.Create(
            ruleName: "Blacklisted IP Address",
            description: "Block transactions from blacklisted IP addresses",
            ruleType: RuleType.BLACKLIST,
            priority: 1,
            action: RuleAction.BLOCK,
            scoreImpact: 100,
            conditionsJson: JsonSerializer.Serialize(new
            {
                entityType = "IP",
                checkType = "BLOCK"
            })
        );
        rules.Add(ipBlacklistRule);

        // 6. Email Blacklist Rule
        var emailBlacklistRule = RiskRule.Create(
            ruleName: "Blacklisted Email Domain",
            description: "Block transactions from blacklisted email domains",
            ruleType: RuleType.BLACKLIST,
            priority: 1,
            action: RuleAction.BLOCK,
            scoreImpact: 100,
            conditionsJson: JsonSerializer.Serialize(new
            {
                entityType = "EMAIL",
                checkType = "BLOCK"
            })
        );
        rules.Add(emailBlacklistRule);

        // 7. Suspicious Email Pattern Rule
        var suspiciousEmailRule = RiskRule.Create(
            ruleName: "Suspicious Email Pattern",
            description: "Flag transactions with suspicious email patterns",
            ruleType: RuleType.ML_MODEL,
            priority: 4,
            action: RuleAction.ADD_SCORE,
            scoreImpact: 20,
            conditionsJson: JsonSerializer.Serialize(new
            {
                patterns = new[] { ".*@.*\\.ru$", ".*@.*\\.cn$", ".*@temp.*\\..*" },
                description = "Suspicious email domain patterns"
            })
        );
        rules.Add(suspiciousEmailRule);

        // 8. New Customer High Value Rule
        var newCustomerRule = RiskRule.Create(
            ruleName: "New Customer High Value",
            description: "Flag high-value transactions from new customers",
            ruleType: RuleType.AMOUNT,
            priority: 5,
            action: RuleAction.ADD_SCORE,
            scoreImpact: 35,
            conditionsJson: JsonSerializer.Serialize(new
            {
                minAmount = 5000,
                maxCustomerAgeDays = 30,
                isNewCustomer = true
            })
        );
        rules.Add(newCustomerRule);

        // 9. International Transaction Rule
        var internationalRule = RiskRule.Create(
            ruleName: "International Transaction",
            description: "Add risk score for international transactions",
            ruleType: RuleType.GEO,
            priority: 6,
            action: RuleAction.ADD_SCORE,
            scoreImpact: 15,
            conditionsJson: JsonSerializer.Serialize(new
            {
                domesticCountry = "US",
                isInternational = true
            })
        );
        rules.Add(internationalRule);

        // 10. Night Transaction Rule
        var nightTransactionRule = RiskRule.Create(
            ruleName: "Unusual Time Transaction",
            description: "Flag transactions during unusual hours (midnight-6am)",
            ruleType: RuleType.ML_MODEL,
            priority: 7,
            action: RuleAction.ADD_SCORE,
            scoreImpact: 10,
            conditionsJson: JsonSerializer.Serialize(new
            {
                startHour = 0,
                endHour = 6,
                timezone = "UTC"
            })
        );
        rules.Add(nightTransactionRule);

        await dbContext.RiskRules.AddRangeAsync(rules);
        await dbContext.SaveChangesAsync();

        return rules;
    }

    private static async Task SeedMerchantRiskProfiles(RiskAssessmentDbContext dbContext)
    {
        var profiles = new List<MerchantRiskProfile>();

        // 1. Low Risk Merchant Profile
        var lowRiskProfile = MerchantRiskProfile.Create(
            merchantId: "merchant_low_risk",
            riskLevel: RiskLevel.LOW
        );
        lowRiskProfile.UpdateThresholds(autoReject: 90, manualReview: 60);
        lowRiskProfile.SetMaxTransactionAmount(50000, "USD");
        profiles.Add(lowRiskProfile);

        // 2. Medium Risk Merchant Profile
        var mediumRiskProfile = MerchantRiskProfile.Create(
            merchantId: "merchant_medium_risk",
            riskLevel: RiskLevel.MEDIUM
        );
        mediumRiskProfile.UpdateThresholds(autoReject: 80, manualReview: 50);
        mediumRiskProfile.SetMaxTransactionAmount(25000, "USD");
        profiles.Add(mediumRiskProfile);

        // 3. High Risk Merchant Profile
        var highRiskProfile = MerchantRiskProfile.Create(
            merchantId: "merchant_high_risk",
            riskLevel: RiskLevel.HIGH
        );
        highRiskProfile.UpdateThresholds(autoReject: 70, manualReview: 40);
        highRiskProfile.SetMaxTransactionAmount(10000, "USD");
        profiles.Add(highRiskProfile);

        // 4. Crypto Merchant Profile (stricter)
        var cryptoProfile = MerchantRiskProfile.Create(
            merchantId: "merchant_crypto",
            riskLevel: RiskLevel.HIGH
        );
        cryptoProfile.UpdateThresholds(autoReject: 60, manualReview: 30);
        cryptoProfile.SetMaxTransactionAmount(5000, "USD");
        profiles.Add(cryptoProfile);

        // 5. International Merchant Profile
        var internationalProfile = MerchantRiskProfile.Create(
            merchantId: "merchant_international",
            riskLevel: RiskLevel.MEDIUM
        );
        internationalProfile.UpdateThresholds(autoReject: 85, manualReview: 55);
        internationalProfile.SetMaxTransactionAmount(15000, "USD");
        profiles.Add(internationalProfile);

        await dbContext.MerchantRiskProfiles.AddRangeAsync(profiles);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedBlacklistEntries(RiskAssessmentDbContext dbContext)
    {
        var blacklistEntries = new List<BlacklistEntry>();

        // Blacklisted IPs
        blacklistEntries.Add(BlacklistEntry.Create(
            entityType: BlacklistEntry.EntityTypeIp,
            entityValue: "192.168.1.100",
            reason: "Known fraudulent activity",
            addedBy: "system"
        ));

        blacklistEntries.Add(BlacklistEntry.Create(
            entityType: BlacklistEntry.EntityTypeIp,
            entityValue: "10.0.0.50",
            reason: "Multiple chargebacks",
            addedBy: "system"
        ));

        // Blacklisted emails
        blacklistEntries.Add(BlacklistEntry.Create(
            entityType: BlacklistEntry.EntityTypeEmail,
            entityValue: "fraud@test.com",
            reason: "Known fraudulent email domain",
            addedBy: "system"
        ));

        blacklistEntries.Add(BlacklistEntry.Create(
            entityType: BlacklistEntry.EntityTypeEmail,
            entityValue: "scam@temporary-mail.com",
            reason: "Temporary email service",
            addedBy: "system"
        ));

        // Blacklisted cards
        blacklistEntries.Add(BlacklistEntry.Create(
            entityType: BlacklistEntry.EntityTypeCard,
            entityValue: "4111111111111111",
            reason: "Stolen card",
            addedBy: "system"
        ));

        // Blacklisted customers
        blacklistEntries.Add(BlacklistEntry.Create(
            entityType: BlacklistEntry.EntityTypeCustomer,
            entityValue: "customer_blacklisted_001",
            reason: "History of fraudulent transactions",
            addedBy: "system"
        ));

        await dbContext.BlacklistEntries.AddRangeAsync(blacklistEntries);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedWhitelistEntries(RiskAssessmentDbContext dbContext)
    {
        var whitelistEntries = new List<WhitelistEntry>();

        // Whitelisted VIP customers
        whitelistEntries.Add(WhitelistEntry.Create(
            entityType: WhitelistEntry.EntityTypeCustomer,
            entityValue: "customer_vip_001",
            reason: "VIP customer with excellent history",
            addedBy: "system"
        ));

        whitelistEntries.Add(WhitelistEntry.Create(
            entityType: WhitelistEntry.EntityTypeCustomer,
            entityValue: "customer_trusted_002",
            reason: "Long-term trusted customer",
            addedBy: "system"
        ));

        // Whitelisted emails (corporate domains)
        whitelistEntries.Add(WhitelistEntry.Create(
            entityType: WhitelistEntry.EntityTypeEmail,
            entityValue: "*@fortune500.com",
            reason: "Corporate email domain",
            addedBy: "system"
        ));

        // Whitelisted IPs (corporate network)
        whitelistEntries.Add(WhitelistEntry.Create(
            entityType: WhitelistEntry.EntityTypeIp,
            entityValue: "172.16.0.0/16",
            reason: "Corporate network range",
            addedBy: "system"
        ));

        // Whitelisted cards
        whitelistEntries.Add(WhitelistEntry.Create(
            entityType: WhitelistEntry.EntityTypeCard,
            entityValue: "4242424242424242",
            reason: "Verified corporate card",
            addedBy: "system"
        ));

        await dbContext.WhitelistEntries.AddRangeAsync(whitelistEntries);
        await dbContext.SaveChangesAsync();
    }
}
