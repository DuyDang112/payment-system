using FluentValidation;
using PaymentRouter.Domain.Models;

namespace PaymentRouter.Features.RoutingRules;

public class CreateRuleRequestValidator : AbstractValidator<CreateRuleRequest>
{
    public CreateRuleRequestValidator()
    {
        RuleFor(x => x.MerchantId)
            .NotEmpty()
            .WithMessage("MerchantId is required")
            .MaximumLength(50)
            .WithMessage("MerchantId must not exceed 50 characters");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Priority must be zero or positive");

        RuleFor(x => x.Conditions)
            .NotNull()
            .WithMessage("Conditions are required");

        RuleFor(x => x.PreferredProviderIds)
            .NotEmpty()
            .WithMessage("At least one preferred provider ID is required");

        RuleFor(x => x.Strategy)
            .IsInEnum()
            .WithMessage("Invalid routing strategy");

        // Validate that if currencies are specified, they're 3-letter codes
        RuleFor(x => x.Conditions.Currencies)
            .Must(currencies => currencies == null || currencies.All(c => c.Length == 3))
            .WithMessage("All currencies must be 3-letter ISO codes")
            .When(x => x.Conditions != null && x.Conditions.Currencies != null);

        // Validate that min amount < max amount
        RuleFor(x => x.Conditions.MinAmount)
            .LessThan(x => x.Conditions.MaxAmount)
            .WithMessage("MinAmount must be less than MaxAmount")
            .When(x => x.Conditions != null && x.Conditions.MinAmount.HasValue && x.Conditions.MaxAmount.HasValue);
    }
}
