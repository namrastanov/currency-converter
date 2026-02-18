using CurrencyConverter.Application.DTOs;
using FluentAssertions;

namespace CurrencyConverter.UnitTests.Application;

public class DtoTests
{
    [Fact]
    public void LatestRatesDto_ShouldHaveCorrectProperties()
    {
        var rates = new Dictionary<string, decimal> { ["EUR"] = 0.85m, ["GBP"] = 0.73m };
        var dto = new LatestRatesDto("USD", new DateTime(2025, 1, 15), rates);

        dto.BaseCurrency.Should().Be("USD");
        dto.Date.Should().Be(new DateTime(2025, 1, 15));
        dto.Rates.Should().HaveCount(2);
        dto.Rates["EUR"].Should().Be(0.85m);
    }

    [Fact]
    public void ConversionResultDto_ShouldHaveCorrectProperties()
    {
        var dto = new ConversionResultDto("USD", "EUR", 100, 85, 0.85m, new DateTime(2025, 1, 15));

        dto.From.Should().Be("USD");
        dto.To.Should().Be("EUR");
        dto.Amount.Should().Be(100);
        dto.Result.Should().Be(85);
        dto.Rate.Should().Be(0.85m);
        dto.Date.Should().Be(new DateTime(2025, 1, 15));
    }

    [Fact]
    public void HistoricalRatesDto_ShouldHaveCorrectProperties()
    {
        var rates = new List<CurrencyConverter.Domain.Models.ExchangeRate>
        {
            new("USD", new DateTime(2025, 1, 15), new Dictionary<string, decimal> { ["EUR"] = 0.85m }),
            new("USD", new DateTime(2025, 1, 16), new Dictionary<string, decimal> { ["EUR"] = 0.86m })
        };

        var dto = new HistoricalRatesDto("USD", rates.AsReadOnly(), 2, 1, 10, 1, false, false);

        dto.BaseCurrency.Should().Be("USD");
        dto.Rates.Should().HaveCount(2);
        dto.Page.Should().Be(1);
        dto.PageSize.Should().Be(10);
        dto.TotalCount.Should().Be(2);
        dto.TotalPages.Should().Be(1);
        dto.HasNextPage.Should().BeFalse();
        dto.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void CurrencyDto_ShouldHaveCorrectProperties()
    {
        var dto = new CurrencyDto("USD", "US Dollar", false);

        dto.Code.Should().Be("USD");
        dto.Name.Should().Be("US Dollar");
        dto.IsRestricted.Should().BeFalse();
    }

    [Fact]
    public void GetLatestRatesQuery_ShouldHaveCorrectProperties()
    {
        var query = new GetLatestRatesQuery("EUR");

        query.BaseCurrency.Should().Be("EUR");
    }

    [Fact]
    public void ConvertCurrencyQuery_ShouldHaveCorrectProperties()
    {
        var query = new ConvertCurrencyQuery("USD", "EUR", 100);

        query.From.Should().Be("USD");
        query.To.Should().Be("EUR");
        query.Amount.Should().Be(100);
    }

    [Fact]
    public void GetHistoricalRatesQuery_ShouldHaveCorrectProperties()
    {
        var from = new DateTime(2025, 1, 1);
        var to = new DateTime(2025, 1, 31);
        var query = new GetHistoricalRatesQuery("USD", from, to, 2, 20);

        query.BaseCurrency.Should().Be("USD");
        query.StartDate.Should().Be(from);
        query.EndDate.Should().Be(to);
        query.Page.Should().Be(2);
        query.PageSize.Should().Be(20);
    }

    [Fact]
    public void GetCurrenciesQuery_ShouldCreateSuccessfully()
    {
        var query = new GetCurrenciesQuery();

        query.Should().NotBeNull();
    }
}
