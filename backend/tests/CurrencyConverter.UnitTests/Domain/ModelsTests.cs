using CurrencyConverter.Domain.Models;
using FluentAssertions;

namespace CurrencyConverter.UnitTests.Domain;

public class ModelsTests
{
    [Fact]
    public void Currency_ShouldHaveCorrectProperties()
    {
        var currency = new Currency("USD", "US Dollar");
        currency.Code.Should().Be("USD");
        currency.Name.Should().Be("US Dollar");
    }

    [Fact]
    public void ExchangeRate_ShouldHaveCorrectProperties()
    {
        var rates = new Dictionary<string, decimal> { ["USD"] = 1.1m, ["GBP"] = 0.85m };
        var date = new DateTime(2024, 1, 15);
        var exchangeRate = new ExchangeRate("EUR", date, rates);

        exchangeRate.BaseCurrency.Should().Be("EUR");
        exchangeRate.Date.Should().Be(date);
        exchangeRate.Rates.Should().HaveCount(2);
    }

    [Fact]
    public void ConversionResult_ShouldHaveCorrectProperties()
    {
        var date = new DateTime(2024, 1, 15);
        var result = new ConversionResult("EUR", "USD", 100m, 110m, 1.1m, date);

        result.From.Should().Be("EUR");
        result.To.Should().Be("USD");
        result.Amount.Should().Be(100m);
        result.Result.Should().Be(110m);
        result.Rate.Should().Be(1.1m);
        result.Date.Should().Be(date);
    }

    [Fact]
    public void PaginatedResult_ShouldHaveCorrectProperties()
    {
        var items = new List<string> { "a", "b", "c" };
        var result = new PaginatedResult<string>(items, 10, 1, 3, 4, true, false);

        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(3);
        result.TotalPages.Should().Be(4);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void PaginatedResult_LastPage_ShouldHaveCorrectFlags()
    {
        var items = new List<string> { "x" };
        var result = new PaginatedResult<string>(items, 10, 4, 3, 4, false, true);

        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }
}
