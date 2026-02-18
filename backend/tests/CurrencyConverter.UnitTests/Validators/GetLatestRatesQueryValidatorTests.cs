using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace CurrencyConverter.UnitTests.Validators;

public class GetLatestRatesQueryValidatorTests
{
    private readonly GetLatestRatesQueryValidator _validator = new();

    [Fact]
    public void Should_Pass_ForValidBaseCurrency()
    {
        var query = new GetLatestRatesQuery("EUR");
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_WhenBaseCurrencyIsEmpty()
    {
        var query = new GetLatestRatesQuery("");
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.BaseCurrency);
    }

    [Fact]
    public void Should_Fail_WhenBaseCurrencyIsNot3Characters()
    {
        var query = new GetLatestRatesQuery("EU");
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.BaseCurrency);
    }

    [Fact]
    public void Should_Fail_WhenBaseCurrencyIsTooLong()
    {
        var query = new GetLatestRatesQuery("EURO");
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.BaseCurrency);
    }

    [Theory]
    [InlineData("TRY")]
    [InlineData("PLN")]
    [InlineData("THB")]
    [InlineData("MXN")]
    public void Should_Fail_WhenBaseCurrencyIsRestricted(string currency)
    {
        var query = new GetLatestRatesQuery(currency);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.BaseCurrency)
            .WithErrorMessage($"Currency '{currency}' is restricted and cannot be used.");
    }
}
