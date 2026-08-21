using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Authentication.Features.Token;

/// <summary>
/// Token request model for token exchange
/// </summary>
public record TokenRequest(
    [Required] string GrantType,
    string? Code,
    string? RefreshToken,
    string? RedirectUri,
    string? ClientId,
    string? ClientSecret,
    string? CodeVerifier,
    string? Scope
);

/// <summary>
/// Validator for TokenRequest
/// </summary>
public class TokenRequestValidator : AbstractValidator<TokenRequest>
{
    public TokenRequestValidator()
    {
        RuleFor(x => x.GrantType)
            .NotEmpty().WithMessage("Grant type is required")
            .Must(BeValidGrantType).WithMessage("Invalid grant type");

        When(x => x.GrantType == "authorization_code", () =>
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Authorization code is required for authorization_code grant");
            RuleFor(x => x.RedirectUri)
                .NotEmpty().WithMessage("Redirect URI is required for authorization_code grant");
        });

        When(x => x.GrantType == "refresh_token", () =>
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token is required for refresh_token grant");
        });

        When(x => x.GrantType == "client_credentials", () =>
        {
            RuleFor(x => x.ClientId)
                .NotEmpty().WithMessage("Client ID is required for client_credentials grant");
            RuleFor(x => x.ClientSecret)
                .NotEmpty().WithMessage("Client secret is required for client_credentials grant");
        });
    }

    private bool BeValidGrantType(string grantType)
    {
        return grantType switch
        {
            "authorization_code" => true,
            "client_credentials" => true,
            "refresh_token" => true,
            "password" => true,
            _ => false
        };
    }
}
