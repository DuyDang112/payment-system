using FluentValidation;
using PaymentRouter.Domain.Models;

namespace PaymentRouter.Features.ProviderManagement;

public class AddProviderRequestValidator : AbstractValidator<AddProviderRequest>
{
    public AddProviderRequestValidator()
    {
        RuleFor(x => x.ProviderId)
            .NotEmpty()
            .WithMessage("ProviderId is required")
            .MaximumLength(50)
            .WithMessage("ProviderId must not exceed 50 characters");

        RuleFor(x => x.ProviderName)
            .NotEmpty()
            .WithMessage("ProviderName is required")
            .MaximumLength(200)
            .WithMessage("ProviderName must not exceed 200 characters");

        RuleFor(x => x.ProviderType)
            .IsInEnum()
            .WithMessage("Invalid provider type");

        RuleFor(x => x.SupportedCurrencies)
            .NotEmpty()
            .WithMessage("At least one supported currency is required");

        RuleFor(x => x.SupportedMethods)
            .NotEmpty()
            .WithMessage("At least one supported payment method is required");

        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Priority must be zero or positive");

        RuleFor(x => x.FixedFee)
            .NotNull()
            .WithMessage("FixedFee is required");

        RuleFor(x => x.FixedFee.Currency)
            .Length(3)
            .WithMessage("FixedFee currency must be a 3-letter ISO code");

        RuleFor(x => x.PercentageFee)
            .GreaterThanOrEqualTo(0)
            .WithMessage("PercentageFee must be zero or positive")
            .LessThanOrEqualTo(100)
            .WithMessage("PercentageFee must not exceed 100");

        RuleFor(x => x.MinAmount)
            .GreaterThan(0)
            .WithMessage("MinAmount must be greater than zero");

        RuleFor(x => x.MaxAmount)
            .GreaterThan(0)
            .WithMessage("MaxAmount must be greater than zero")
            .GreaterThan(x => x.MinAmount)
            .WithMessage("MaxAmount must be greater than MinAmount");
    }
}
