using FluentValidation;
using FluentValidation.AspNetCore;
using System.ComponentModel.DataAnnotations;

namespace Authentication.Features.Login;

/// <summary>
/// Login request model
/// </summary>
public record LoginRequest(
    [Required] string Username,
    [Required] string Password,
    bool RememberMe = false
);

/// <summary>
/// Validator for LoginRequest
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .EmailAddress().WithMessage("Username must be a valid email address")
            .MaximumLength(100).WithMessage("Username cannot exceed 100 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long")
            .MaximumLength(100).WithMessage("Password cannot exceed 100 characters");
    }
}
