namespace PaymentProcessing.Domain.Models;

/// <summary>
/// Represents the status of a payment attempt
/// </summary>
public enum AttemptStatus
{
    Started,
    Completed,
    Failed
}
