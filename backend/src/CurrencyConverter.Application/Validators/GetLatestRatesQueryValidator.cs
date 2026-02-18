using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Domain.Rules;
using FluentValidation;

namespace CurrencyConverter.Application.Validators;

public class GetLatestRatesQueryValidator : AbstractValidator<GetLatestRatesQuery>
{
    public GetLatestRatesQueryValidator()
    {
        RuleFor(x => x.BaseCurrency)
            .NotEmpty().WithMessage("Base currency is required.")
            .Length(3).WithMessage("Currency code must be exactly 3 characters.")
            .Must(code => !CurrencyRestrictions.IsRestricted(code))
            .WithMessage(x => $"Currency '{x.BaseCurrency}' is restricted and cannot be used.");
    }
}
