namespace PaymentProcessing.Domain.Models;

/// <summary>
/// Represents the status of a payment
/// </summary>
public enum PaymentStatus
{
    Created,
    Processing,
    RiskEvaluation,
    Routing,
    Authorizing,
    Completed,
    Failed,
    Cancelled
}
