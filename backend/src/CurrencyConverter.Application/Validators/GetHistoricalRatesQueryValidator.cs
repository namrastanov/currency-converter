using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Domain.Rules;
using FluentValidation;

namespace CurrencyConverter.Application.Validators;

public class GetHistoricalRatesQueryValidator : AbstractValidator<GetHistoricalRatesQuery>
{
    private const int MaxDateRangeDays = 730;

    public GetHistoricalRatesQueryValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.BaseCurrency)
            .NotEmpty().WithMessage("Base currency is required.")
            .Length(3).WithMessage("Currency code must be exactly 3 characters.")
            .Must(code => !CurrencyRestrictions.IsRestricted(code))
            .WithMessage(x => $"Currency '{x.BaseCurrency}' is restricted and cannot be used.");

        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(x => x.EndDate)
            .WithMessage("Start date must be less than or equal to end date.");

        RuleFor(x => x.EndDate)
            .Must((query, endDate) =>
            {
                var utcNow = timeProvider.GetUtcNow();
                var utcToday = utcNow.UtcDateTime.Date;
                if (endDate.Date <= utcToday)
                    return true;

                var userToday = utcNow.UtcDateTime.AddMinutes(-query.TimezoneOffset).Date;
                return endDate.Date <= userToday;
            })
            .WithMessage("End date cannot be in the future.");

        RuleFor(x => x.TimezoneOffset)
            .InclusiveBetween(-720, 840)
            .WithMessage("Timezone offset must be between -720 and 840 minutes.");

        RuleFor(x => x)
            .Must(x => (x.EndDate - x.StartDate).TotalDays <= MaxDateRangeDays)
            .WithMessage($"Date range cannot exceed {MaxDateRangeDays} days (2 years).")
            .WithName("DateRange");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}
