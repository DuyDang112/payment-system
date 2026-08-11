namespace PaymentProcessing.Domain.Models;

/// <summary>
/// Represents a monetary value with currency
/// </summary>
public sealed record Money(decimal Amount, string Currency)
{
    public static Money Of(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Money amount cannot be negative", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency cannot be empty", nameof(currency));
        }

        return new Money(amount, currency);
    }
}
