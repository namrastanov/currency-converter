using System.Net;
using System.Text.Json;
using CurrencyConverter.Domain.Exceptions;
using CurrencyConverter.Infrastructure.Providers.Frankfurter;
using FluentAssertions;
using Moq;
using Moq.Protected;

namespace CurrencyConverter.UnitTests.Infrastructure;

public class FrankfurterProviderTests
{
    private static HttpClient CreateMockHttpClient(HttpResponseMessage response)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var client = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://api.frankfurter.dev")
        };

        return client;
    }

    [Fact]
    public async Task GetCurrenciesAsync_ShouldReturnCurrencies()
    {
        var responseData = new Dictionary<string, string>
        {
            ["EUR"] = "Euro",
            ["USD"] = "US Dollar"
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseData))
        };

        var client = CreateMockHttpClient(response);
        var provider = new FrankfurterProvider(client);

        var result = await provider.GetCurrenciesAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Code == "EUR" && c.Name == "Euro");
    }

    [Fact]
    public async Task GetLatestRatesAsync_ShouldConstructCorrectUrl()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("/latest?base=EUR")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    amount = 1,
                    @base = "EUR",
                    date = "2024-01-15",
                    rates = new Dictionary<string, decimal> { ["USD"] = 1.1m }
                }))
            });

        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.frankfurter.dev") };
        var provider = new FrankfurterProvider(client);

        var result = await provider.GetLatestRatesAsync("EUR");

        result.BaseCurrency.Should().Be("EUR");
        result.Rates.Should().ContainKey("USD");
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ShouldConstructCorrectUrl()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("/2024-01-01..2024-01-31")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    amount = 1,
                    @base = "EUR",
                    start_date = "2024-01-01",
                    end_date = "2024-01-31",
                    rates = new Dictionary<string, Dictionary<string, decimal>>
                    {
                        ["2024-01-02"] = new() { ["USD"] = 1.1m }
                    }
                }))
            });

        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.frankfurter.dev") };
        var provider = new FrankfurterProvider(client);

        var result = await provider.GetHistoricalRatesAsync("EUR", new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));

        result.Should().HaveCount(1);
        result[0].BaseCurrency.Should().Be("EUR");
    }

    [Fact]
    public async Task Should_ThrowExternalApiException_OnHttpError()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server Error")
        };

        var client = CreateMockHttpClient(response);
        var provider = new FrankfurterProvider(client);

        await provider.Invoking(p => p.GetCurrenciesAsync())
            .Should().ThrowAsync<ExternalApiException>()
            .Where(e => e.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void ProviderName_ShouldBeFrankfurter()
    {
        var client = CreateMockHttpClient(new HttpResponseMessage(HttpStatusCode.OK));
        var provider = new FrankfurterProvider(client);

        provider.ProviderName.Should().Be("Frankfurter");
    }
}
