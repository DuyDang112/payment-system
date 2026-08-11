using FluentValidation;

namespace RiskAssessment.Features.EvaluateRisk;

/// <summary>
/// Validator for EvaluateRiskRequest
/// </summary>
public sealed class EvaluateRiskRequestValidator : AbstractValidator<EvaluateRiskRequest>
{
    public EvaluateRiskRequestValidator()
    {
        RuleFor(request => request.PaymentId)
            .NotEmpty()
            .WithMessage("Payment ID is required")
            .MaximumLength(100)
            .WithMessage("Payment ID cannot exceed 100 characters");

        RuleFor(request => request.MerchantId)
            .NotEmpty()
            .WithMessage("Merchant ID is required")
            .MaximumLength(100)
            .WithMessage("Merchant ID cannot exceed 100 characters");

        RuleFor(request => request.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required")
            .MaximumLength(100)
            .WithMessage("Customer ID cannot exceed 100 characters");

        RuleFor(request => request.CustomerEmail)
            .EmailAddress()
            .WithMessage("Customer email must be valid")
            .When(request => !string.IsNullOrEmpty(request.CustomerEmail));

        RuleFor(request => request.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .When(request => request.Amount.HasValue);

        RuleFor(request => request.Currency)
            .Length(3)
            .WithMessage("Currency must be 3 characters (ISO 4217)")
            .When(request => !string.IsNullOrEmpty(request.Currency));

        RuleFor(request => request.CountryCode)
            .Length(2)
            .WithMessage("Country code must be 2 characters (ISO 3166)")
            .When(request => !string.IsNullOrEmpty(request.CountryCode));

        RuleFor(request => request.IpAddress)
            .Must(BeValidIpAddress)
            .WithMessage("IP address must be valid")
            .When(request => !string.IsNullOrEmpty(request.IpAddress));
    }

    private static bool BeValidIpAddress(string? ipAddress)
    {
        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }
}
