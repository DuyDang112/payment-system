using Microsoft.EntityFrameworkCore;
using PaymentProcessing.Domain.Models;

namespace PaymentProcessing.Infrastructure.Data;

/// <summary>
/// Seed data for Payment Processing database
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Seeds the database with sample data
    /// </summary>
    public static async Task SeedAsync(PaymentsDbContext dbContext)
    {
        // Ensure database is created
        await dbContext.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (await dbContext.Payments.AnyAsync())
        {
            return; // Database already seeded
        }

        var merchantId = "merchant_001";
        var customerId = "customer_001";

        // Sample Payment 1: Completed payment
        var completedPayment = Payment.Create(
            paymentId: "pay_completed_001",
            merchantId: merchantId,
            customerId: customerId,
            money: Money.Of(100.50m, "USD"),
            idempotencyKey: "idemp_completed_001",
            paymentMethodToken: "tok_visa_completed"
        );

        // Transition through states to Completed
        completedPayment.TransitionTo(PaymentStatus.Processing);
        completedPayment.TransitionTo(PaymentStatus.RiskEvaluation);
        completedPayment.TransitionTo(PaymentStatus.Routing);
        completedPayment.TransitionTo(PaymentStatus.Authorizing);
        completedPayment.MarkAsCompleted();
        completedPayment.RecordAttempt("stripe", AttemptStatus.Completed);

        // Sample Payment 2: Failed payment (insufficient funds)
        var failedPayment = Payment.Create(
            paymentId: "pay_failed_001",
            merchantId: merchantId,
            customerId: customerId,
            money: Money.Of(2500.00m, "USD"),
            idempotencyKey: "idemp_failed_001",
            paymentMethodToken: "tok_visa_insufficient"
        );

        failedPayment.TransitionTo(PaymentStatus.Processing);
        failedPayment.TransitionTo(PaymentStatus.RiskEvaluation);
        failedPayment.TransitionTo(PaymentStatus.Routing);
        failedPayment.TransitionTo(PaymentStatus.Authorizing);
        failedPayment.MarkAsFailed("Insufficient funds");
        failedPayment.RecordAttempt("stripe", AttemptStatus.Failed, "Insufficient funds");

        // Sample Payment 3: Payment with retry attempts
        var retriedPayment = Payment.Create(
            paymentId: "pay_retry_001",
            merchantId: merchantId,
            customerId: customerId,
            money: Money.Of(75.25m, "EUR"),
            idempotencyKey: "idemp_retry_001",
            paymentMethodToken: "tok_mastercard_retry"
        );

        retriedPayment.TransitionTo(PaymentStatus.Processing);
        retriedPayment.TransitionTo(PaymentStatus.RiskEvaluation);
        retriedPayment.TransitionTo(PaymentStatus.Routing);

        // First attempt failed
        retriedPayment.RecordAttempt("adyen", AttemptStatus.Failed, "Timeout");
        // Second attempt succeeded
        retriedPayment.RecordAttempt("adyen", AttemptStatus.Completed);
        retriedPayment.TransitionTo(PaymentStatus.Authorizing);
        retriedPayment.MarkAsCompleted();

        // Sample Payment 4: Cancelled payment
        var cancelledPayment = Payment.Create(
            paymentId: "pay_cancelled_001",
            merchantId: merchantId,
            customerId: customerId,
            money: Money.Of(50.00m, "GBP"),
            idempotencyKey: "idemp_cancelled_001",
            paymentMethodToken: "tok_visa_cancel"
        );

        cancelledPayment.TransitionTo(PaymentStatus.Processing);
        cancelledPayment.TransitionTo(PaymentStatus.RiskEvaluation);
        cancelledPayment.Cancel();

        // Sample Payment 5: Payment still in processing
        var processingPayment = Payment.Create(
            paymentId: "pay_processing_001",
            merchantId: merchantId,
            customerId: customerId,
            money: Money.Of(1250.75m, "CAD"),
            idempotencyKey: "idemp_processing_001",
            paymentMethodToken: "tok_amex_processing"
        );

        processingPayment.TransitionTo(PaymentStatus.Processing);
        processingPayment.TransitionTo(PaymentStatus.RiskEvaluation);
        processingPayment.RecordAttempt("braintree", AttemptStatus.Started);

        // Sample Payment 6: High-value completed payment
        var highValuePayment = Payment.Create(
            paymentId: "pay_high_value_001",
            merchantId: merchantId,
            customerId: customerId,
            money: Money.Of(15000.00m, "USD"),
            idempotencyKey: "idemp_high_value_001",
            paymentMethodToken: "tok_wire_high_value"
        );

        highValuePayment.TransitionTo(PaymentStatus.Processing);
        highValuePayment.TransitionTo(PaymentStatus.RiskEvaluation);
        highValuePayment.TransitionTo(PaymentStatus.Routing);
        highValuePayment.TransitionTo(PaymentStatus.Authorizing);
        highValuePayment.MarkAsCompleted();
        highValuePayment.RecordAttempt("stripe", AttemptStatus.Completed);

        // Sample Payment 7: Failed risk evaluation
        var riskFailedPayment = Payment.Create(
            paymentId: "pay_risk_failed_001",
            merchantId: merchantId,
            customerId: "customer_risk_001",
            money: Money.Of(9999.99m, "USD"),
            idempotencyKey: "idemp_risk_failed_001",
            paymentMethodToken: "tok_risk_fail"
        );

        riskFailedPayment.TransitionTo(PaymentStatus.Processing);
        riskFailedPayment.TransitionTo(PaymentStatus.RiskEvaluation);
        riskFailedPayment.MarkAsFailed("Risk evaluation failed: Suspicious activity detected");

        // Sample Payment 8: Multi-currency completed payment
        var multiCurrencyPayment = Payment.Create(
            paymentId: "pay_jpy_001",
            merchantId: "merchant_japan_001",
            customerId: "customer_japan_001",
            money: Money.Of(50000m, "JPY"),
            idempotencyKey: "idemp_jpy_001",
            paymentMethodToken: "tok_jp_bank"
        );

        multiCurrencyPayment.TransitionTo(PaymentStatus.Processing);
        multiCurrencyPayment.TransitionTo(PaymentStatus.RiskEvaluation);
        multiCurrencyPayment.TransitionTo(PaymentStatus.Routing);
        multiCurrencyPayment.TransitionTo(PaymentStatus.Authorizing);
        multiCurrencyPayment.MarkAsCompleted();
        multiCurrencyPayment.RecordAttempt("sbpg", AttemptStatus.Completed);

        // Sample Payment 9: Failed routing (no provider available)
        var routingFailedPayment = Payment.Create(
            paymentId: "pay_routing_failed_001",
            merchantId: merchantId,
            customerId: customerId,
            money: Money.Of(25.00m, "AUD"),
            idempotencyKey: "idemp_routing_failed_001",
            paymentMethodToken: "tok_unsupported"
        );

        routingFailedPayment.TransitionTo(PaymentStatus.Processing);
        routingFailedPayment.TransitionTo(PaymentStatus.RiskEvaluation);
        routingFailedPayment.TransitionTo(PaymentStatus.Routing);
        routingFailedPayment.MarkAsFailed("No payment provider available for this payment method");
        routingFailedPayment.RecordAttempt("stripe", AttemptStatus.Failed, "Unsupported payment method");

        // Sample Payment 10: Recurring payment pattern
        for (int i = 1; i <= 5; i++)
        {
            var recurringPayment = Payment.Create(
                paymentId: $"pay_recurring_{i:D3}",
                merchantId: merchantId,
                customerId: customerId,
                money: Money.Of(29.99m, "USD"),
                idempotencyKey: $"idemp_recurring_{i:D3}",
                paymentMethodToken: "tok_subscription_recurring"
            );

            recurringPayment.TransitionTo(PaymentStatus.Processing);
            recurringPayment.TransitionTo(PaymentStatus.RiskEvaluation);
            recurringPayment.TransitionTo(PaymentStatus.Routing);
            recurringPayment.TransitionTo(PaymentStatus.Authorizing);
            recurringPayment.MarkAsCompleted();
            recurringPayment.RecordAttempt("stripe", AttemptStatus.Completed);

            await dbContext.Payments.AddAsync(recurringPayment);
        }

        // Add all the main payments
        await dbContext.Payments.AddAsync(completedPayment);
        await dbContext.Payments.AddAsync(failedPayment);
        await dbContext.Payments.AddAsync(retriedPayment);
        await dbContext.Payments.AddAsync(cancelledPayment);
        await dbContext.Payments.AddAsync(processingPayment);
        await dbContext.Payments.AddAsync(highValuePayment);
        await dbContext.Payments.AddAsync(riskFailedPayment);
        await dbContext.Payments.AddAsync(multiCurrencyPayment);
        await dbContext.Payments.AddAsync(routingFailedPayment);

        // Create idempotency keys for all payments
        var idempotencyKeys = new[]
        {
            IdempotencyKey.Create("idemp_completed_001", merchantId, completedPayment.Id, expiresInHours: 48),
            IdempotencyKey.Create("idemp_failed_001", merchantId, failedPayment.Id, expiresInHours: 48),
            IdempotencyKey.Create("idemp_retry_001", merchantId, retriedPayment.Id, expiresInHours: 48),
            IdempotencyKey.Create("idemp_cancelled_001", merchantId, cancelledPayment.Id, expiresInHours: 48),
            IdempotencyKey.Create("idemp_processing_001", merchantId, processingPayment.Id, expiresInHours: 48),
            IdempotencyKey.Create("idemp_high_value_001", merchantId, highValuePayment.Id, expiresInHours: 48),
            IdempotencyKey.Create("idemp_risk_failed_001", merchantId, riskFailedPayment.Id, expiresInHours: 48),
            IdempotencyKey.Create("idemp_jpy_001", "merchant_japan_001", multiCurrencyPayment.Id, expiresInHours: 48),
            IdempotencyKey.Create("idemp_routing_failed_001", merchantId, routingFailedPayment.Id, expiresInHours: 48)
        };

        // Add idempotency keys for recurring payments
        for (int i = 1; i <= 5; i++)
        {
            idempotencyKeys = idempotencyKeys.Append(
                IdempotencyKey.Create($"idemp_recurring_{i:D3}", merchantId, Guid.NewGuid(), expiresInHours: 48)
            ).ToArray();
        }

        await dbContext.IdempotencyKeys.AddRangeAsync(idempotencyKeys);

        // Save all changes
        await dbContext.SaveChangesAsync();

        Console.WriteLine($"✅ Seeded {14} payments with various states");
        Console.WriteLine($"✅ Seeded {idempotencyKeys.Length} idempotency keys");
        Console.WriteLine($"✅ Sample payment states:");
        Console.WriteLine($"   - Completed: 7 payments");
        Console.WriteLine($"   - Failed: 3 payments");
        Console.WriteLine($"   - Processing: 1 payment");
        Console.WriteLine($"   - Cancelled: 1 payment");
        Console.WriteLine($"   - Currencies: USD, EUR, GBP, CAD, JPY, AUD");
    }
}
