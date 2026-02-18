using System.Net;
using System.Text.Json;
using CurrencyConverter.Infrastructure.Providers.Frankfurter;
using FluentAssertions;
using Moq;
using Moq.Protected;

namespace CurrencyConverter.UnitTests.Infrastructure;

public class FrankfurterProviderAdditionalTests
{
    private FrankfurterProvider CreateProviderWithResponse(HttpStatusCode statusCode, string content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.frankfurter.dev") };
        return new FrankfurterProvider(httpClient);
    }

    [Fact]
    public async Task ConvertAsync_ShouldReturnConversionResult()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            amount = 100,
            @base = "USD",
            date = "2025-01-15",
            rates = new Dictionary<string, decimal> { ["EUR"] = 85m }
        });

        var provider = CreateProviderWithResponse(HttpStatusCode.OK, responseJson);
        var result = await provider.ConvertAsync("USD", "EUR", 100);

        result.From.Should().Be("USD");
        result.To.Should().Be("EUR");
        result.Amount.Should().Be(100);
        result.Result.Should().Be(85m);
        result.Rate.Should().Be(0.85m);
    }

    [Fact]
    public async Task ConvertAsync_ShouldHandleZeroAmount()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            amount = 0,
            @base = "USD",
            date = "2025-01-15",
            rates = new Dictionary<string, decimal> { ["EUR"] = 0m }
        });

        var provider = CreateProviderWithResponse(HttpStatusCode.OK, responseJson);
        var result = await provider.ConvertAsync("USD", "EUR", 0);

        result.Rate.Should().Be(0);
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ShouldReturnOrderedRates()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            amount = 1,
            @base = "USD",
            start_date = "2025-01-01",
            end_date = "2025-01-03",
            rates = new Dictionary<string, Dictionary<string, decimal>>
            {
                ["2025-01-03"] = new() { ["EUR"] = 0.87m },
                ["2025-01-01"] = new() { ["EUR"] = 0.85m },
                ["2025-01-02"] = new() { ["EUR"] = 0.86m }
            }
        });

        var provider = CreateProviderWithResponse(HttpStatusCode.OK, responseJson);
        var result = await provider.GetHistoricalRatesAsync("USD", new DateTime(2025, 1, 1), new DateTime(2025, 1, 3));

        result.Should().HaveCount(3);
        result[0].Date.Should().Be(new DateTime(2025, 1, 1));
        result[1].Date.Should().Be(new DateTime(2025, 1, 2));
        result[2].Date.Should().Be(new DateTime(2025, 1, 3));
    }
}
