using PaymentProcessing.Shared;

namespace PaymentProcessing.Features.Shared.Errors;

/// <summary>
/// Payment-specific error definitions
/// </summary>
public static class PaymentErrors
{
    public static readonly Error AlreadyExists = new(
        "Payment.AlreadyExists",
        "A payment with this identifier already exists");

    public static readonly Error NotFound = new(
        "Payment.NotFound",
        "Payment not found");

    public static readonly Error InvalidState = new(
        "Payment.InvalidState",
        "Payment state transition is invalid");

    public static readonly Error InvalidAmount = new(
        "Payment.InvalidAmount",
        "Payment amount must be positive");

    public static readonly Error InvalidCurrency = new(
        "Payment.InvalidCurrency",
        "Currency is not supported");

    public static readonly Error InvalidPaymentMethod = new(
        "Payment.InvalidPaymentMethod",
        "Payment method is not supported");

    public static readonly Error IdempotencyKeyExpired = new(
        "Payment.IdempotencyKeyExpired",
        "Idempotency key has expired");

    public static readonly Error IdempotencyKeyConflict = new(
        "Payment.IdempotencyKeyConflict",
        "Idempotency key conflict detected");

    public static readonly Error MaxRetriesExceeded = new(
        "Payment.MaxRetriesExceeded",
        "Maximum retry attempts exceeded");

    public static readonly Error CannotCancelCompleted = new(
        "Payment.CannotCancelCompleted",
        "Cannot cancel a completed payment");

    public static readonly Error CannotCancelFailed = new(
        "Payment.CannotCancelFailed",
        "Cannot cancel a failed payment");

    public static Error AlreadyExistsWithId(string paymentId) => new(
        "Payment.AlreadyExists",
        $"Payment '{paymentId}' already exists");

    public static Error NotFoundWithId(string paymentId) => new(
        "Payment.NotFound",
        $"Payment '{paymentId}' not found");

    public static readonly Error RiskAssessmentFailed = new(
        "Payment.RiskAssessmentFailed",
        "Risk assessment service failed to respond");

    public static readonly Error RiskAssessmentRejected = new(
        "Payment.RiskAssessmentRejected",
        "Payment was rejected by risk assessment");

    public static readonly Error PaymentRoutingFailed = new(
        "Payment.PaymentRoutingFailed",
        "Payment routing service failed to respond");

    public static readonly Error BankAuthorizationFailed = new(
        "Payment.BankAuthorizationFailed",
        "Bank authorization failed");
}
