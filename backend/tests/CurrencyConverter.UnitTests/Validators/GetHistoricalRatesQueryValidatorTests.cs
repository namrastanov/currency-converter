using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace CurrencyConverter.UnitTests.Validators;

public class GetHistoricalRatesQueryValidatorTests
{
    private readonly GetHistoricalRatesQueryValidator _validator = new(TimeProvider.System);

    [Fact]
    public void Should_Pass_ForValidQuery()
    {
        var query = new GetHistoricalRatesQuery("EUR", DateTime.UtcNow.Date.AddDays(-30), DateTime.UtcNow.Date.AddDays(-1), 1, 10);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_WhenBaseCurrencyIsEmpty()
    {
        var query = new GetHistoricalRatesQuery("", DateTime.UtcNow.Date.AddDays(-30), DateTime.UtcNow.Date.AddDays(-1));
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.BaseCurrency);
    }

    [Fact]
    public void Should_Fail_WhenBaseCurrencyIsRestricted()
    {
        var query = new GetHistoricalRatesQuery("TRY", DateTime.UtcNow.Date.AddDays(-30), DateTime.UtcNow.Date.AddDays(-1));
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.BaseCurrency);
    }

    [Fact]
    public void Should_Fail_WhenStartDateIsAfterEndDate()
    {
        var query = new GetHistoricalRatesQuery("EUR", DateTime.UtcNow.Date.AddDays(-1), DateTime.UtcNow.Date.AddDays(-30));
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.StartDate);
    }

    [Fact]
    public void Should_Fail_WhenEndDateIsInFuture()
    {
        var query = new GetHistoricalRatesQuery("EUR", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1));
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void Should_Fail_WhenDateRangeExceeds730Days()
    {
        var query = new GetHistoricalRatesQuery("EUR", DateTime.UtcNow.Date.AddDays(-800), DateTime.UtcNow.Date.AddDays(-1));
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor("DateRange");
    }

    [Fact]
    public void Should_Fail_WhenPageIsZero()
    {
        var query = new GetHistoricalRatesQuery("EUR", DateTime.UtcNow.Date.AddDays(-30), DateTime.UtcNow.Date.AddDays(-1), 0, 10);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Fact]
    public void Should_Fail_WhenPageSizeIsZero()
    {
        var query = new GetHistoricalRatesQuery("EUR", DateTime.UtcNow.Date.AddDays(-30), DateTime.UtcNow.Date.AddDays(-1), 1, 0);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Should_Fail_WhenPageSizeExceeds100()
    {
        var query = new GetHistoricalRatesQuery("EUR", DateTime.UtcNow.Date.AddDays(-30), DateTime.UtcNow.Date.AddDays(-1), 1, 101);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Should_Pass_WhenPageSizeIs100()
    {
        var query = new GetHistoricalRatesQuery("EUR", DateTime.UtcNow.Date.AddDays(-30), DateTime.UtcNow.Date.AddDays(-1), 1, 100);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
    }
}
