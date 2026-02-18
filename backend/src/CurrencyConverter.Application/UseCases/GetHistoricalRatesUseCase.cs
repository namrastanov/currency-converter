using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Settings;
using CurrencyConverter.Domain.Exceptions;
using CurrencyConverter.Domain.Models;
using CurrencyConverter.Domain.Rules;
using Microsoft.Extensions.Options;

namespace CurrencyConverter.Application.UseCases;

public class GetHistoricalRatesUseCase
{
    private readonly ICurrencyProviderFactory _providerFactory;
    private readonly ICacheService _cacheService;
    private readonly CacheSettings _cacheSettings;
    private readonly TimeProvider _timeProvider;

    public GetHistoricalRatesUseCase(
        ICurrencyProviderFactory providerFactory,
        ICacheService cacheService,
        IOptions<CacheSettings> cacheSettings,
        TimeProvider timeProvider)
    {
        _providerFactory = providerFactory;
        _cacheService = cacheService;
        _cacheSettings = cacheSettings.Value;
        _timeProvider = timeProvider;
    }

    public async Task<HistoricalRatesDto> ExecuteAsync(GetHistoricalRatesQuery query, CancellationToken cancellationToken = default)
    {
        if (CurrencyRestrictions.IsRestricted(query.BaseCurrency))
            throw new CurrencyNotSupportedException(query.BaseCurrency);

        var baseCurrency = query.BaseCurrency.ToUpperInvariant();
        var today = _timeProvider.GetUtcNow().UtcDateTime.Date;

        var fetchedDates = await _cacheService.GetCachedDatesAsync(baseCurrency, query.StartDate, query.EndDate, cancellationToken);

        var allDates = GenerateAllDates(query.StartDate, query.EndDate);

        var unfetchedDates = allDates
            .Where(d => !fetchedDates.Contains(d) || d == today)
            .ToHashSet();

        if (unfetchedDates.Count > 0)
        {
            var gaps = GroupIntoContiguousRanges(unfetchedDates);
            var mergedGaps = MergeGaps(gaps, _cacheSettings.GapMergeThresholdDays);

            var provider = _providerFactory.GetProvider();

            foreach (var (start, end) in mergedGaps)
            {
                var historicalRates = await provider.GetHistoricalRatesAsync(baseCurrency, start, end, cancellationToken);

                foreach (var rate in historicalRates)
                {
                    await _cacheService.StoreDateRatesAsync(baseCurrency, rate.Date, rate.Rates, cancellationToken);
                }

                var allGapDates = GenerateAllDates(start, end).Where(d => d != today);
                await _cacheService.MarkDatesAsFetchedAsync(baseCurrency, allGapDates, cancellationToken);
            }
        }

        var datesWithData = allDates
            .Where(d => !unfetchedDates.Contains(d) || d != today || unfetchedDates.Count > 0)
            .ToList();

        var allRatesData = await _cacheService.GetDateRatesBatchAsync(baseCurrency, allDates, cancellationToken);

        var exchangeRates = allRatesData
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new ExchangeRate(
                baseCurrency,
                kvp.Key,
                kvp.Value
                    .Where(r => !CurrencyRestrictions.IsRestricted(r.Key))
                    .ToDictionary(r => r.Key, r => r.Value)))
            .ToList();

        var totalCount = exchangeRates.Count;
        var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);
        var pagedRates = exchangeRates
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new HistoricalRatesDto(
            baseCurrency,
            pagedRates,
            totalCount,
            query.Page,
            query.PageSize,
            totalPages,
            query.Page < totalPages,
            query.Page > 1);
    }

    internal static List<DateTime> GenerateAllDates(DateTime start, DateTime end)
    {
        var dates = new List<DateTime>();
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            dates.Add(date);
        }
        return dates;
    }

    internal static List<(DateTime Start, DateTime End)> GroupIntoContiguousRanges(IEnumerable<DateTime> dates)
    {
        var sorted = dates.OrderBy(d => d).ToList();
        if (sorted.Count == 0)
            return new List<(DateTime, DateTime)>();

        var ranges = new List<(DateTime Start, DateTime End)>();
        var rangeStart = sorted[0];
        var rangeEnd = sorted[0];

        for (int i = 1; i < sorted.Count; i++)
        {
            if ((sorted[i] - rangeEnd).Days <= 1)
            {
                rangeEnd = sorted[i];
            }
            else
            {
                ranges.Add((rangeStart, rangeEnd));
                rangeStart = sorted[i];
                rangeEnd = sorted[i];
            }
        }

        ranges.Add((rangeStart, rangeEnd));
        return ranges;
    }

    internal static List<(DateTime Start, DateTime End)> MergeGaps(
        List<(DateTime Start, DateTime End)> gaps,
        int mergeThresholdDays)
    {
        if (gaps.Count <= 1)
            return gaps;

        var merged = new List<(DateTime Start, DateTime End)> { gaps[0] };

        for (int i = 1; i < gaps.Count; i++)
        {
            var last = merged[^1];
            var bridgeDays = (gaps[i].Start - last.End).Days - 1;

            if (bridgeDays <= mergeThresholdDays)
            {
                merged[^1] = (last.Start, gaps[i].End);
            }
            else
            {
                merged.Add(gaps[i]);
            }
        }

        return merged;
    }
}
