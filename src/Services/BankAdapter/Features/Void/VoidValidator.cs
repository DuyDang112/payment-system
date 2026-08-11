using FluentValidation;

namespace BankAdapter.Features.Void;

/// <summary>
/// Validator for void requests
/// </summary>
public sealed class VoidRequestValidator : AbstractValidator<VoidRequest>
{
    public VoidRequestValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty().WithMessage("Payment ID is required")
            .MaximumLength(200).WithMessage("Payment ID cannot exceed 200 characters");

        RuleFor(x => x.ProviderId)
            .NotEmpty().WithMessage("Provider ID is required")
            .MaximumLength(200).WithMessage("Provider ID cannot exceed 200 characters");

        RuleFor(x => x.AuthorizationToken)
            .NotEmpty().WithMessage("Authorization token is required")
            .MaximumLength(500).WithMessage("Authorization token cannot exceed 500 characters");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(1000000).WithMessage("Amount cannot exceed 1,000,000");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a valid ISO 4217 currency code (3 characters)")
            .Must(BeUpperCase).WithMessage("Currency must be in uppercase (e.g., USD, EUR)");

        RuleFor(x => x.Metadata)
            .NotNull().WithMessage("Metadata cannot be null");
    }

    private static bool BeUpperCase(string currency)
    {
        return currency.All(char.IsUpper);
    }
}
