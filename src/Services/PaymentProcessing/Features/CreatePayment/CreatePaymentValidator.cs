using FluentValidation;
using PaymentProcessing.Features.Shared.Errors;

namespace PaymentProcessing.Features.CreatePayment;

/// <summary>
/// Validator for CreatePaymentRequest
/// </summary>
public sealed class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    private static readonly string[] SupportedCurrencies = { "USD", "EUR", "GBP", "CAD", "AUD" };

    public CreatePaymentRequestValidator()
    {
        RuleFor(request => request.MerchantId)
            .NotEmpty()
            .WithMessage("Merchant ID is required");

        RuleFor(request => request.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(request => request.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero");

        RuleFor(request => request.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Must(BeSupportedCurrency)
            .WithMessage("Currency is not supported");

        RuleFor(request => request.IdempotencyKey)
            .NotEmpty()
            .WithMessage("Idempotency key is required")
            .MaximumLength(255)
            .WithMessage("Idempotency key cannot exceed 255 characters");
    }

    private static bool BeSupportedCurrency(string currency)
    {
        return SupportedCurrencies.Contains(currency.ToUpperInvariant());
    }
}
