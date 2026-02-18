using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Settings;
using CurrencyConverter.Application.UseCases;
using CurrencyConverter.Domain.Exceptions;
using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace CurrencyConverter.UnitTests.UseCases;

public class GetLatestRatesUseCaseTests
{
    private readonly Mock<ICurrencyProviderFactory> _providerFactory = new();
    private readonly Mock<ICurrencyProvider> _provider = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly GetLatestRatesUseCase _useCase;

    public GetLatestRatesUseCaseTests()
    {
        _providerFactory.Setup(f => f.GetProvider(null)).Returns(_provider.Object);
        _useCase = new GetLatestRatesUseCase(
            _providerFactory.Object,
            _cacheService.Object,
            Options.Create(new CacheSettings()));
    }

    [Fact]
    public async Task Should_ThrowCurrencyNotSupportedException_WhenBaseIsRestricted()
    {
        var query = new GetLatestRatesQuery("TRY");

        await _useCase.Invoking(u => u.ExecuteAsync(query))
            .Should().ThrowAsync<CurrencyNotSupportedException>();
    }

    [Fact]
    public async Task Should_ReturnCached_WhenCacheHit()
    {
        var cached = new LatestRatesDto("EUR", DateTime.UtcNow, new Dictionary<string, decimal> { ["USD"] = 1.1m });
        _cacheService.Setup(c => c.GetAsync<LatestRatesDto>("rates:latest:EUR", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _useCase.ExecuteAsync(new GetLatestRatesQuery("EUR"));

        result.Should().Be(cached);
        _provider.Verify(p => p.GetLatestRatesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_FetchAndCache_WhenCacheMiss()
    {
        _cacheService.Setup(c => c.GetAsync<LatestRatesDto>("rates:latest:EUR", It.IsAny<CancellationToken>()))
            .ReturnsAsync((LatestRatesDto?)null);

        var exchangeRate = new ExchangeRate("EUR", DateTime.UtcNow, new Dictionary<string, decimal>
        {
            ["USD"] = 1.1m,
            ["GBP"] = 0.85m,
            ["TRY"] = 35.5m
        });
        _provider.Setup(p => p.GetLatestRatesAsync("EUR", It.IsAny<CancellationToken>()))
            .ReturnsAsync(exchangeRate);

        var result = await _useCase.ExecuteAsync(new GetLatestRatesQuery("EUR"));

        result.Rates.Should().NotContainKey("TRY");
        result.Rates.Should().ContainKey("USD");
        result.Rates.Should().ContainKey("GBP");

        _cacheService.Verify(c => c.SetAsync(
            "rates:latest:EUR",
            It.IsAny<LatestRatesDto>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_FilterRestrictedCurrenciesFromRates()
    {
        _cacheService.Setup(c => c.GetAsync<LatestRatesDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LatestRatesDto?)null);

        var exchangeRate = new ExchangeRate("EUR", DateTime.UtcNow, new Dictionary<string, decimal>
        {
            ["USD"] = 1.1m,
            ["PLN"] = 4.5m,
            ["THB"] = 38m,
            ["MXN"] = 20m
        });
        _provider.Setup(p => p.GetLatestRatesAsync("EUR", It.IsAny<CancellationToken>()))
            .ReturnsAsync(exchangeRate);

        var result = await _useCase.ExecuteAsync(new GetLatestRatesQuery("EUR"));

        result.Rates.Should().HaveCount(1);
        result.Rates.Should().ContainKey("USD");
    }
}
