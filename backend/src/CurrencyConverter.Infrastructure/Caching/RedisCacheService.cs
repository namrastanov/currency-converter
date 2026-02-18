using System.Text.Json;
using CurrencyConverter.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CurrencyConverter.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly TimeProvider _timeProvider;
    private int _consecutiveErrors;
    private const int ErrorEscalationThreshold = 5;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger,
        TimeProvider timeProvider)
    {
        _distributedCache = distributedCache;
        _redis = redis;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    private void OnSuccess()
    {
        Interlocked.Exchange(ref _consecutiveErrors, 0);
    }

    private void OnError(Exception ex, string message, params object[] args)
    {
        var errorCount = Interlocked.Increment(ref _consecutiveErrors);
        if (errorCount >= ErrorEscalationThreshold)
            _logger.LogError(ex, "[Redis degraded — {ErrorCount} consecutive failures] " + message, [errorCount, .. args]);
        else
            _logger.LogWarning(ex, message, args);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var data = await _distributedCache.GetStringAsync(key, cancellationToken);
            OnSuccess();
            if (data is null) return null;
            return JsonSerializer.Deserialize<T>(data, JsonOptions);
        }
        catch (Exception ex)
        {
            OnError(ex, "Failed to get cache key {CacheKey}. Returning null.", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            var options = new DistributedCacheEntryOptions();
            if (ttl.HasValue)
                options.AbsoluteExpirationRelativeToNow = ttl;

            await _distributedCache.SetStringAsync(key, json, options, cancellationToken);
            OnSuccess();
        }
        catch (Exception ex)
        {
            OnError(ex, "Failed to set cache key {CacheKey}.", key);
        }
    }

    public async Task<IReadOnlySet<DateTime>> GetCachedDatesAsync(
        string baseCurrency, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"rates:historical:fetched:{baseCurrency.ToUpperInvariant()}";
            var startScore = double.Parse(startDate.ToString("yyyyMMdd"));
            var endScore = double.Parse(endDate.ToString("yyyyMMdd"));

            var members = await db.SortedSetRangeByScoreAsync(key, startScore, endScore);
            OnSuccess();

            var dates = new HashSet<DateTime>();
            foreach (var member in members)
            {
                if (DateTime.TryParse(member.ToString(), out var date))
                    dates.Add(date.Date);
            }

            return dates;
        }
        catch (Exception ex)
        {
            OnError(ex, "Failed to get cached dates for {BaseCurrency}. Returning empty set.", baseCurrency);
            return new HashSet<DateTime>();
        }
    }

    public async Task StoreDateRatesAsync(
        string baseCurrency, DateTime date, Dictionary<string, decimal> rates, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"rates:historical:{baseCurrency.ToUpperInvariant()}:{date:yyyy-MM-dd}";
            var json = JsonSerializer.Serialize(rates, JsonOptions);

            await db.StringSetAsync(key, json);
            OnSuccess();
        }
        catch (Exception ex)
        {
            OnError(ex, "Failed to store date rates for {BaseCurrency} {Date}.", baseCurrency, date);
        }
    }

    public async Task MarkDatesAsFetchedAsync(
        string baseCurrency, IEnumerable<DateTime> dates, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"rates:historical:fetched:{baseCurrency.ToUpperInvariant()}";
            var today = _timeProvider.GetUtcNow().UtcDateTime.Date;

            var entries = dates
                .Where(d => d.Date != today)
                .Select(d => new SortedSetEntry(d.ToString("yyyy-MM-dd"), double.Parse(d.ToString("yyyyMMdd"))))
                .ToArray();

            if (entries.Length > 0)
                await db.SortedSetAddAsync(key, entries);

            OnSuccess();
        }
        catch (Exception ex)
        {
            OnError(ex, "Failed to mark dates as fetched for {BaseCurrency}.", baseCurrency);
        }
    }

    public async Task<Dictionary<DateTime, Dictionary<string, decimal>>> GetDateRatesBatchAsync(
        string baseCurrency, IEnumerable<DateTime> dates, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<DateTime, Dictionary<string, decimal>>();

        try
        {
            var db = _redis.GetDatabase();
            var dateList = dates.ToList();

            var batch = db.CreateBatch();
            var tasks = new Dictionary<DateTime, Task<RedisValue>>();

            foreach (var date in dateList)
            {
                var key = $"rates:historical:{baseCurrency.ToUpperInvariant()}:{date:yyyy-MM-dd}";
                tasks[date] = batch.StringGetAsync(key);
            }

            batch.Execute();
            await Task.WhenAll(tasks.Values);
            OnSuccess();

            foreach (var (date, task) in tasks)
            {
                var value = await task;
                if (!value.IsNullOrEmpty)
                {
                    var rates = JsonSerializer.Deserialize<Dictionary<string, decimal>>(value.ToString(), JsonOptions);
                    if (rates is not null)
                        result[date] = rates;
                }
            }
        }
        catch (Exception ex)
        {
            OnError(ex, "Failed to batch get date rates for {BaseCurrency}. Returning empty.", baseCurrency);
        }

        return result;
    }
}
