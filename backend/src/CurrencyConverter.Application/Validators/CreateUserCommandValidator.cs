using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Domain.Constants;
using FluentValidation;

namespace CurrencyConverter.Application.Validators;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(50).WithMessage("Username must not exceed 50 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(r => r == AppRoles.Admin || r == AppRoles.User)
            .WithMessage($"Role must be '{AppRoles.Admin}' or '{AppRoles.User}'.");
    }
}
