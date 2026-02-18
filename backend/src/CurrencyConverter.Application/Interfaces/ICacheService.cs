namespace CurrencyConverter.Application.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class;

    Task<IReadOnlySet<DateTime>> GetCachedDatesAsync(string baseCurrency, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    Task StoreDateRatesAsync(string baseCurrency, DateTime date, Dictionary<string, decimal> rates, CancellationToken cancellationToken = default);

    Task MarkDatesAsFetchedAsync(string baseCurrency, IEnumerable<DateTime> dates, CancellationToken cancellationToken = default);

    Task<Dictionary<DateTime, Dictionary<string, decimal>>> GetDateRatesBatchAsync(string baseCurrency, IEnumerable<DateTime> dates, CancellationToken cancellationToken = default);
}
