using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Settings;
using CurrencyConverter.Domain.Exceptions;
using CurrencyConverter.Domain.Rules;
using Microsoft.Extensions.Options;

namespace CurrencyConverter.Application.UseCases;

public class GetLatestRatesUseCase
{
    private readonly ICurrencyProviderFactory _providerFactory;
    private readonly ICacheService _cacheService;
    private readonly CacheSettings _cacheSettings;

    public GetLatestRatesUseCase(
        ICurrencyProviderFactory providerFactory,
        ICacheService cacheService,
        IOptions<CacheSettings> cacheSettings)
    {
        _providerFactory = providerFactory;
        _cacheService = cacheService;
        _cacheSettings = cacheSettings.Value;
    }

    public async Task<LatestRatesDto> ExecuteAsync(GetLatestRatesQuery query, CancellationToken cancellationToken = default)
    {
        if (CurrencyRestrictions.IsRestricted(query.BaseCurrency))
            throw new CurrencyNotSupportedException(query.BaseCurrency);

        var cacheKey = $"rates:latest:{query.BaseCurrency.ToUpperInvariant()}";

        var cached = await _cacheService.GetAsync<LatestRatesDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var provider = _providerFactory.GetProvider();
        var exchangeRate = await provider.GetLatestRatesAsync(query.BaseCurrency, cancellationToken);

        var filteredRates = exchangeRate.Rates
            .Where(kvp => !CurrencyRestrictions.IsRestricted(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var result = new LatestRatesDto(exchangeRate.BaseCurrency, exchangeRate.Date, filteredRates);

        var cacheTtl = TimeSpan.FromMinutes(_cacheSettings.LatestRatesTtlMinutes);
        await _cacheService.SetAsync(cacheKey, result, cacheTtl, cancellationToken);

        return result;
    }
}
