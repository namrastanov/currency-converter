using System.Text;
using System.Text.Json;
using CurrencyConverter.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace CurrencyConverter.UnitTests.Infrastructure;

public class RedisCacheServiceAdditionalTests
{
    private readonly Mock<IDistributedCache> _distributedCache = new();
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<ILogger<RedisCacheService>> _logger = new();
    private readonly RedisCacheService _service;

    public RedisCacheServiceAdditionalTests()
    {
        _service = new RedisCacheService(_distributedCache.Object, _redis.Object, _logger.Object, TimeProvider.System);
    }

    [Fact]
    public async Task GetAsync_ShouldDeserializeAndReturn_WhenDataExists()
    {
        var expected = new TestData { Name = "Test", Value = 42 };
        var json = JsonSerializer.Serialize(expected, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var bytes = Encoding.UTF8.GetBytes(json);

        _distributedCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytes);

        var result = await _service.GetAsync<TestData>("test-key");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task SetAsync_ShouldCallSetString_WithCorrectParameters()
    {
        var data = new TestData { Name = "Test", Value = 42 };

        await _service.SetAsync("test-key", data, TimeSpan.FromMinutes(5));

        _distributedCache.Verify(c => c.SetAsync(
            "test-key",
            It.IsAny<byte[]>(),
            It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(5)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_ShouldWorkWithoutTtl()
    {
        var data = new TestData { Name = "Test", Value = 42 };

        await _service.SetAsync("test-key", data);

        _distributedCache.Verify(c => c.SetAsync(
            "test-key",
            It.IsAny<byte[]>(),
            It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StoreDateRatesAsync_ShouldNotThrow_WhenRedisThrows()
    {
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        await _service.Invoking(s => s.StoreDateRatesAsync(
            "EUR", DateTime.UtcNow, new Dictionary<string, decimal> { ["USD"] = 1.1m }))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task MarkDatesAsFetchedAsync_ShouldNotThrow_WhenRedisThrows()
    {
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var dates = new[] { DateTime.UtcNow.Date.AddDays(-1), DateTime.UtcNow.Date.AddDays(-2) };

        await _service.Invoking(s => s.MarkDatesAsFetchedAsync("EUR", dates))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetCachedDatesAsync_ShouldReturnDates_WhenRedisHasData()
    {
        var db = new Mock<IDatabase>();
        var date1 = DateTime.UtcNow.Date.AddDays(-1);
        var date2 = DateTime.UtcNow.Date.AddDays(-2);

        db.Setup(d => d.SortedSetRangeByScoreAsync(
            It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<Exclude>(), It.IsAny<Order>(), It.IsAny<long>(), It.IsAny<long>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue[]
            {
                date1.ToString("yyyy-MM-dd"),
                date2.ToString("yyyy-MM-dd")
            });

        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        var result = await _service.GetCachedDatesAsync("EUR", date2, date1);

        result.Should().HaveCount(2);
        result.Should().Contain(date1);
        result.Should().Contain(date2);
    }

    [Fact]
    public async Task StoreDateRatesAsync_ShouldNotThrow_OnHappyPath()
    {
        var db = new Mock<IDatabase>();
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        await _service.Invoking(s => s.StoreDateRatesAsync("EUR", DateTime.UtcNow.Date.AddDays(-1),
            new Dictionary<string, decimal> { ["USD"] = 1.1m }))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task MarkDatesAsFetchedAsync_ShouldNotThrow_OnHappyPath()
    {
        var db = new Mock<IDatabase>();
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        var dates = new[] { DateTime.UtcNow.Date.AddDays(-1), DateTime.UtcNow.Date.AddDays(-2) };

        await _service.Invoking(s => s.MarkDatesAsFetchedAsync("EUR", dates))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task MarkDatesAsFetchedAsync_ShouldNotThrow_ForTodaysDates()
    {
        var db = new Mock<IDatabase>();
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        var dates = new[] { DateTime.UtcNow.Date };

        await _service.Invoking(s => s.MarkDatesAsFetchedAsync("EUR", dates))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetDateRatesBatchAsync_ShouldReturnRates_WhenCached()
    {
        var db = new Mock<IDatabase>();
        var batch = new Mock<IBatch>();
        var date1 = DateTime.UtcNow.Date.AddDays(-1);
        var ratesJson = JsonSerializer.Serialize(new Dictionary<string, decimal> { ["EUR"] = 0.85m },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        batch.Setup(b => b.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult((RedisValue)ratesJson));

        db.Setup(d => d.CreateBatch(It.IsAny<object>())).Returns(batch.Object);
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        var result = await _service.GetDateRatesBatchAsync("USD", new[] { date1 });

        result.Should().ContainKey(date1);
        result[date1].Should().ContainKey("EUR");
    }

    private class TestData
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
