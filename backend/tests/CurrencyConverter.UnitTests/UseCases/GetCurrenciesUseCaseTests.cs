using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Settings;
using CurrencyConverter.Application.UseCases;
using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace CurrencyConverter.UnitTests.UseCases;

public class GetCurrenciesUseCaseTests
{
    private readonly Mock<ICurrencyProviderFactory> _providerFactory = new();
    private readonly Mock<ICurrencyProvider> _provider = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly GetCurrenciesUseCase _useCase;

    public GetCurrenciesUseCaseTests()
    {
        _providerFactory.Setup(f => f.GetProvider(null)).Returns(_provider.Object);
        _useCase = new GetCurrenciesUseCase(
            _providerFactory.Object,
            _cacheService.Object,
            Options.Create(new CacheSettings()));
    }

    [Fact]
    public async Task Should_ReturnCachedCurrencies_WhenCacheHit()
    {
        var cached = new List<CurrencyDto> { new("EUR", "Euro", false), new("USD", "US Dollar", false) };
        _cacheService.Setup(c => c.GetAsync<List<CurrencyDto>>("currencies:list", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _useCase.ExecuteAsync();

        result.Should().BeEquivalentTo(cached);
        _provider.Verify(p => p.GetCurrenciesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_FetchFromProvider_WhenCacheMiss()
    {
        _cacheService.Setup(c => c.GetAsync<List<CurrencyDto>>("currencies:list", It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<CurrencyDto>?)null);

        var currencies = new List<Currency>
        {
            new("EUR", "Euro"),
            new("USD", "US Dollar"),
            new("TRY", "Turkish Lira")
        };
        _provider.Setup(p => p.GetCurrenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currencies.AsReadOnly());

        var result = await _useCase.ExecuteAsync();

        result.Should().HaveCount(3);
        result.Should().Contain(c => c.Code == "TRY" && c.IsRestricted);
        result.Should().Contain(c => c.Code == "EUR" && !c.IsRestricted);
    }

    [Fact]
    public async Task Should_MarkRestrictedCurrencies()
    {
        _cacheService.Setup(c => c.GetAsync<List<CurrencyDto>>("currencies:list", It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<CurrencyDto>?)null);

        var currencies = new List<Currency>
        {
            new("PLN", "Polish Zloty"),
            new("THB", "Thai Baht"),
            new("MXN", "Mexican Peso"),
            new("GBP", "British Pound")
        };
        _provider.Setup(p => p.GetCurrenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currencies.AsReadOnly());

        var result = await _useCase.ExecuteAsync();

        result.Where(c => c.IsRestricted).Should().HaveCount(3);
        result.First(c => c.Code == "GBP").IsRestricted.Should().BeFalse();
    }
}
