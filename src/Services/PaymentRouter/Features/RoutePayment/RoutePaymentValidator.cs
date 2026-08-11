using FluentValidation;
using PaymentRouter.Domain.Models;

namespace PaymentRouter.Features.RoutePayment;

public class RoutePaymentRequestValidator : AbstractValidator<RoutePaymentRequest>
{
    public RoutePaymentRequestValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("PaymentId is required")
            .MaximumLength(50)
            .WithMessage("PaymentId must not exceed 50 characters");

        RuleFor(x => x.MerchantId)
            .NotEmpty()
            .WithMessage("MerchantId is required")
            .MaximumLength(50)
            .WithMessage("MerchantId must not exceed 50 characters");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Amount must not exceed 1,000,000");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be a 3-letter ISO code");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum()
            .WithMessage("Invalid payment method");

        RuleFor(x => x.CountryCode)
            .Length(2)
            .WithMessage("CountryCode must be a 2-letter ISO code")
            .When(x => !string.IsNullOrEmpty(x.CountryCode));

        RuleFor(x => x.Strategy)
            .IsInEnum()
            .WithMessage("Invalid routing strategy");
    }
}
