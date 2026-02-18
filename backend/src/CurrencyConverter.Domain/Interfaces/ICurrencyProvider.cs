using CurrencyConverter.Domain.Models;

namespace CurrencyConverter.Domain.Interfaces;

public interface ICurrencyProvider
{
    string ProviderName { get; }

    Task<IReadOnlyList<Currency>> GetCurrenciesAsync(CancellationToken cancellationToken = default);

    Task<ExchangeRate> GetLatestRatesAsync(string baseCurrency, CancellationToken cancellationToken = default);

    Task<ConversionResult> ConvertAsync(string from, string to, decimal amount, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExchangeRate>> GetHistoricalRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
