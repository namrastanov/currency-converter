using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Domain.Rules;
using FluentValidation;

namespace CurrencyConverter.Application.Validators;

public class ConvertCurrencyQueryValidator : AbstractValidator<ConvertCurrencyQuery>
{
    public ConvertCurrencyQueryValidator()
    {
        RuleFor(x => x.From)
            .NotEmpty().WithMessage("Source currency is required.")
            .Length(3).WithMessage("Source currency code must be exactly 3 characters.")
            .Must(code => !CurrencyRestrictions.IsRestricted(code))
            .WithMessage(x => $"Currency '{x.From}' is restricted and cannot be used.");

        RuleFor(x => x.To)
            .NotEmpty().WithMessage("Target currency is required.")
            .Length(3).WithMessage("Target currency code must be exactly 3 characters.")
            .Must(code => !CurrencyRestrictions.IsRestricted(code))
            .WithMessage(x => $"Currency '{x.To}' is restricted and cannot be used.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");
    }
}
