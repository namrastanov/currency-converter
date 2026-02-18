using System.Net;
using System.Net.Http.Json;
using CurrencyConverter.Domain.Exceptions;
using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.Domain.Models;
using CurrencyConverter.Infrastructure.Providers.Frankfurter.DTOs;

namespace CurrencyConverter.Infrastructure.Providers.Frankfurter;

public class FrankfurterProvider : ICurrencyProvider
{
    private readonly HttpClient _httpClient;

    public string ProviderName => "Frankfurter";

    public FrankfurterProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<Currency>> GetCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/v1/currencies", cancellationToken);
        await EnsureSuccessResponse(response);

        var currencies = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken)
            ?? throw new ExternalApiException(ProviderName, HttpStatusCode.InternalServerError, "Failed to deserialize currencies response.");

        return currencies
            .Select(kvp => new Currency(kvp.Key, kvp.Value))
            .OrderBy(c => c.Code)
            .ToList()
            .AsReadOnly();
    }

    public async Task<ExchangeRate> GetLatestRatesAsync(string baseCurrency, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/v1/latest?base={baseCurrency.ToUpperInvariant()}", cancellationToken);
        await EnsureSuccessResponse(response);

        var data = await response.Content.ReadFromJsonAsync<FrankfurterLatestResponse>(cancellationToken)
            ?? throw new ExternalApiException(ProviderName, HttpStatusCode.InternalServerError, "Failed to deserialize latest rates response.");

        return new ExchangeRate(data.Base, data.Date, data.Rates);
    }

    public async Task<ConversionResult> ConvertAsync(string from, string to, decimal amount, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"/v1/latest?from={from.ToUpperInvariant()}&to={to.ToUpperInvariant()}&amount={amount}",
            cancellationToken);
        await EnsureSuccessResponse(response);

        var data = await response.Content.ReadFromJsonAsync<FrankfurterLatestResponse>(cancellationToken)
            ?? throw new ExternalApiException(ProviderName, HttpStatusCode.InternalServerError, "Failed to deserialize conversion response.");

        var targetRate = data.Rates.GetValueOrDefault(to.ToUpperInvariant());
        var rate = amount > 0 ? targetRate / amount : 0;

        return new ConversionResult(from.ToUpperInvariant(), to.ToUpperInvariant(), amount, targetRate, rate, data.Date);
    }

    public async Task<IReadOnlyList<ExchangeRate>> GetHistoricalRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var start = startDate.ToString("yyyy-MM-dd");
        var end = endDate.ToString("yyyy-MM-dd");

        var response = await _httpClient.GetAsync(
            $"/v1/{start}..{end}?base={baseCurrency.ToUpperInvariant()}",
            cancellationToken);
        await EnsureSuccessResponse(response);

        var data = await response.Content.ReadFromJsonAsync<FrankfurterTimeSeriesResponse>(cancellationToken)
            ?? throw new ExternalApiException(ProviderName, HttpStatusCode.InternalServerError, "Failed to deserialize historical rates response.");

        return data.Rates
            .Select(kvp => new ExchangeRate(
                data.Base,
                DateTime.Parse(kvp.Key),
                kvp.Value))
            .OrderBy(r => r.Date)
            .ToList()
            .AsReadOnly();
    }

    private async Task EnsureSuccessResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync();
        throw new ExternalApiException(
            ProviderName,
            response.StatusCode,
            $"Frankfurter API returned {(int)response.StatusCode}: {content}");
    }
}
