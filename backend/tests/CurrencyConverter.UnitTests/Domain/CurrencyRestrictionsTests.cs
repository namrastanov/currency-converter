using CurrencyConverter.Domain.Rules;
using FluentAssertions;

namespace CurrencyConverter.UnitTests.Domain;

public class CurrencyRestrictionsTests
{
    [Theory]
    [InlineData("TRY")]
    [InlineData("PLN")]
    [InlineData("THB")]
    [InlineData("MXN")]
    public void IsRestricted_ShouldReturnTrue_ForExcludedCurrencies(string currency)
    {
        CurrencyRestrictions.IsRestricted(currency).Should().BeTrue();
    }

    [Theory]
    [InlineData("try")]
    [InlineData("pln")]
    [InlineData("Thb")]
    [InlineData("mxn")]
    public void IsRestricted_ShouldBeCaseInsensitive(string currency)
    {
        CurrencyRestrictions.IsRestricted(currency).Should().BeTrue();
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("CHF")]
    public void IsRestricted_ShouldReturnFalse_ForValidCurrencies(string currency)
    {
        CurrencyRestrictions.IsRestricted(currency).Should().BeFalse();
    }

    [Fact]
    public void GetExcludedCurrencies_ShouldReturnFourCurrencies()
    {
        var excluded = CurrencyRestrictions.GetExcludedCurrencies();
        excluded.Should().HaveCount(4);
        excluded.Should().Contain(new[] { "TRY", "PLN", "THB", "MXN" });
    }
}
