using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Settings;
using CurrencyConverter.Domain.Rules;
using Microsoft.Extensions.Options;

namespace CurrencyConverter.Application.UseCases;

public class GetCurrenciesUseCase
{
    private const string CacheKey = "currencies:list";

    private readonly ICurrencyProviderFactory _providerFactory;
    private readonly ICacheService _cacheService;
    private readonly CacheSettings _cacheSettings;

    public GetCurrenciesUseCase(
        ICurrencyProviderFactory providerFactory,
        ICacheService cacheService,
        IOptions<CacheSettings> cacheSettings)
    {
        _providerFactory = providerFactory;
        _cacheService = cacheService;
        _cacheSettings = cacheSettings.Value;
    }

    public async Task<IReadOnlyList<CurrencyDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cacheService.GetAsync<List<CurrencyDto>>(CacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var provider = _providerFactory.GetProvider();
        var currencies = await provider.GetCurrenciesAsync(cancellationToken);

        var result = currencies
            .Select(c => new CurrencyDto(c.Code, c.Name, CurrencyRestrictions.IsRestricted(c.Code)))
            .OrderBy(c => c.Code)
            .ToList();

        var cacheTtl = TimeSpan.FromMinutes(_cacheSettings.CurrenciesTtlMinutes);
        await _cacheService.SetAsync(CacheKey, result, cacheTtl, cancellationToken);

        return result;
    }
}
