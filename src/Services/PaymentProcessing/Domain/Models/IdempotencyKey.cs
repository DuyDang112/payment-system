namespace PaymentProcessing.Domain.Models;

/// <summary>
/// Represents an idempotency key for duplicate prevention
/// </summary>
public sealed class IdempotencyKey
{
    public Guid Id { get; private set; }
    public string Key { get; private set; }
    public string MerchantId { get; private set; }
    public Guid PaymentId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private IdempotencyKey() { }

    public static IdempotencyKey Create(string key, string merchantId, Guid paymentId, int expiresInHours = 24)
    {
        var now = DateTime.UtcNow;
        return new IdempotencyKey
        {
            Id = Guid.NewGuid(),
            Key = key,
            MerchantId = merchantId,
            PaymentId = paymentId,
            CreatedAt = now,
            ExpiresAt = now.AddHours(expiresInHours)
        };
    }

    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpiresAt;
    }
}
