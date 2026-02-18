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

public class GetHistoricalRatesUseCaseTests
{
    private readonly Mock<ICurrencyProviderFactory> _providerFactory = new();
    private readonly Mock<ICurrencyProvider> _provider = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly GetHistoricalRatesUseCase _useCase;

    public GetHistoricalRatesUseCaseTests()
    {
        _providerFactory.Setup(f => f.GetProvider(null)).Returns(_provider.Object);
        var options = Options.Create(new CacheSettings { GapMergeThresholdDays = 5 });
        _useCase = new GetHistoricalRatesUseCase(
            _providerFactory.Object, _cacheService.Object, options, TimeProvider.System);
    }

    [Fact]
    public async Task Should_ThrowCurrencyNotSupportedException_WhenBaseIsRestricted()
    {
        var query = new GetHistoricalRatesQuery("TRY", DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-1));

        await _useCase.Invoking(u => u.ExecuteAsync(query))
            .Should().ThrowAsync<CurrencyNotSupportedException>();
    }

    [Fact]
    public async Task Should_ReturnPaginatedResults()
    {
        var start = DateTime.UtcNow.Date.AddDays(-5);
        var end = DateTime.UtcNow.Date.AddDays(-1);
        var query = new GetHistoricalRatesQuery("EUR", start, end, 1, 2);

        _cacheService.Setup(c => c.GetCachedDatesAsync("EUR", start, end, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<DateTime>());

        var historicalRates = new List<ExchangeRate>();
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            historicalRates.Add(new ExchangeRate("EUR", d, new Dictionary<string, decimal> { ["USD"] = 1.1m }));
        }
        _provider.Setup(p => p.GetHistoricalRatesAsync("EUR", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(historicalRates.AsReadOnly());

        _cacheService.Setup(c => c.GetDateRatesBatchAsync("EUR", It.IsAny<IEnumerable<DateTime>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(historicalRates.ToDictionary(r => r.Date, r => r.Rates));

        var result = await _useCase.ExecuteAsync(query);

        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.Rates.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task Should_ReturnEmpty_WhenNoDatesInRange()
    {
        var start = DateTime.UtcNow.Date.AddDays(-2);
        var end = DateTime.UtcNow.Date.AddDays(-1);
        var query = new GetHistoricalRatesQuery("EUR", start, end);

        _cacheService.Setup(c => c.GetCachedDatesAsync("EUR", start, end, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<DateTime>());

        _provider.Setup(p => p.GetHistoricalRatesAsync("EUR", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExchangeRate>().AsReadOnly());

        _cacheService.Setup(c => c.GetDateRatesBatchAsync("EUR", It.IsAny<IEnumerable<DateTime>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateTime, Dictionary<string, decimal>>());

        var result = await _useCase.ExecuteAsync(query);

        result.TotalCount.Should().Be(0);
        result.Rates.Should().BeEmpty();
    }

    [Fact]
    public void GenerateAllDates_ShouldReturnCorrectRange()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 1, 5);

        var dates = GetHistoricalRatesUseCase.GenerateAllDates(start, end);

        dates.Should().HaveCount(5);
        dates.First().Should().Be(start);
        dates.Last().Should().Be(end);
    }

    [Fact]
    public void GroupIntoContiguousRanges_ShouldGroupCorrectly()
    {
        var dates = new List<DateTime>
        {
            new(2024, 1, 1),
            new(2024, 1, 2),
            new(2024, 1, 3),
            new(2024, 1, 10),
            new(2024, 1, 11)
        };

        var ranges = GetHistoricalRatesUseCase.GroupIntoContiguousRanges(dates);

        ranges.Should().HaveCount(2);
        ranges[0].Should().Be((new DateTime(2024, 1, 1), new DateTime(2024, 1, 3)));
        ranges[1].Should().Be((new DateTime(2024, 1, 10), new DateTime(2024, 1, 11)));
    }

    [Fact]
    public void GroupIntoContiguousRanges_EmptyInput_ShouldReturnEmpty()
    {
        var ranges = GetHistoricalRatesUseCase.GroupIntoContiguousRanges(Enumerable.Empty<DateTime>());
        ranges.Should().BeEmpty();
    }

    [Fact]
    public void MergeGaps_ShouldMergeWhenBridgeLessThanThreshold()
    {
        var gaps = new List<(DateTime Start, DateTime End)>
        {
            (new DateTime(2024, 1, 1), new DateTime(2024, 1, 3)),
            (new DateTime(2024, 1, 7), new DateTime(2024, 1, 10))
        };

        var merged = GetHistoricalRatesUseCase.MergeGaps(gaps, 5);

        merged.Should().HaveCount(1);
        merged[0].Start.Should().Be(new DateTime(2024, 1, 1));
        merged[0].End.Should().Be(new DateTime(2024, 1, 10));
    }

    [Fact]
    public void MergeGaps_ShouldNotMerge_WhenBridgeGreaterThanThreshold()
    {
        var gaps = new List<(DateTime Start, DateTime End)>
        {
            (new DateTime(2024, 1, 1), new DateTime(2024, 1, 3)),
            (new DateTime(2024, 1, 15), new DateTime(2024, 1, 20))
        };

        var merged = GetHistoricalRatesUseCase.MergeGaps(gaps, 5);

        merged.Should().HaveCount(2);
    }

    [Fact]
    public void MergeGaps_ShouldHandleMultipleMerges()
    {
        var gaps = new List<(DateTime Start, DateTime End)>
        {
            (new DateTime(2024, 1, 1), new DateTime(2024, 1, 2)),
            (new DateTime(2024, 1, 5), new DateTime(2024, 1, 6)),
            (new DateTime(2024, 1, 9), new DateTime(2024, 1, 10)),
            (new DateTime(2024, 2, 1), new DateTime(2024, 2, 5))
        };

        var merged = GetHistoricalRatesUseCase.MergeGaps(gaps, 5);

        merged.Should().HaveCount(2);
        merged[0].Start.Should().Be(new DateTime(2024, 1, 1));
        merged[0].End.Should().Be(new DateTime(2024, 1, 10));
        merged[1].Start.Should().Be(new DateTime(2024, 2, 1));
    }

    [Fact]
    public void MergeGaps_SingleGap_ShouldReturnAsIs()
    {
        var gaps = new List<(DateTime Start, DateTime End)>
        {
            (new DateTime(2024, 1, 1), new DateTime(2024, 1, 5))
        };

        var merged = GetHistoricalRatesUseCase.MergeGaps(gaps, 5);

        merged.Should().HaveCount(1);
    }
}
