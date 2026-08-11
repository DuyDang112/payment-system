using FluentValidation;

namespace PaymentProcessing.Features.CancelPayment;

/// <summary>
/// Validator for CancelPaymentRequest
/// </summary>
public sealed class CancelPaymentRequestValidator : AbstractValidator<CancelPaymentRequest>
{
    public CancelPaymentRequestValidator()
    {
        RuleFor(request => request.Reason)
            .NotEmpty()
            .WithMessage("Cancellation reason is required")
            .MaximumLength(500)
            .WithMessage("Cancellation reason cannot exceed 500 characters");
    }
}
