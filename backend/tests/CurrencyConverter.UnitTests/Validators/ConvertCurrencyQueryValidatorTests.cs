using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace CurrencyConverter.UnitTests.Validators;

public class ConvertCurrencyQueryValidatorTests
{
    private readonly ConvertCurrencyQueryValidator _validator = new();

    [Fact]
    public void Should_Pass_ForValidQuery()
    {
        var query = new ConvertCurrencyQuery("EUR", "USD", 100);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_WhenFromIsEmpty()
    {
        var query = new ConvertCurrencyQuery("", "USD", 100);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.From);
    }

    [Fact]
    public void Should_Fail_WhenToIsEmpty()
    {
        var query = new ConvertCurrencyQuery("EUR", "", 100);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.To);
    }

    [Fact]
    public void Should_Fail_WhenFromIsNot3Characters()
    {
        var query = new ConvertCurrencyQuery("EU", "USD", 100);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.From);
    }

    [Fact]
    public void Should_Fail_WhenToIsNot3Characters()
    {
        var query = new ConvertCurrencyQuery("EUR", "US", 100);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.To);
    }

    [Fact]
    public void Should_Fail_WhenAmountIsZero()
    {
        var query = new ConvertCurrencyQuery("EUR", "USD", 0);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Should_Fail_WhenAmountIsNegative()
    {
        var query = new ConvertCurrencyQuery("EUR", "USD", -50);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Should_Fail_WhenFromIsRestricted()
    {
        var query = new ConvertCurrencyQuery("TRY", "USD", 100);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.From);
    }

    [Fact]
    public void Should_Fail_WhenToIsRestricted()
    {
        var query = new ConvertCurrencyQuery("EUR", "PLN", 100);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.To);
    }
}
