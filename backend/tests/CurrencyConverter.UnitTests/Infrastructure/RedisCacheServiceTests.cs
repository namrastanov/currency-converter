using CurrencyConverter.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace CurrencyConverter.UnitTests.Infrastructure;

public class RedisCacheServiceTests
{
    private readonly Mock<IDistributedCache> _distributedCache = new();
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<ILogger<RedisCacheService>> _logger = new();
    private readonly RedisCacheService _service;

    public RedisCacheServiceTests()
    {
        _service = new RedisCacheService(_distributedCache.Object, _redis.Object, _logger.Object, TimeProvider.System);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenNotInCache()
    {
        _distributedCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var result = await _service.GetAsync<string>("test-key");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenRedisThrows()
    {
        _distributedCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var result = await _service.GetAsync<string>("test-key");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldNotThrow_WhenRedisFails()
    {
        _distributedCache.Setup(c => c.SetAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        await _service.Invoking(s => s.SetAsync("key", "value", TimeSpan.FromMinutes(5)))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetCachedDatesAsync_ShouldReturnEmptySet_WhenRedisThrows()
    {
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var result = await _service.GetCachedDatesAsync("EUR", DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDateRatesBatchAsync_ShouldReturnEmptyDict_WhenRedisThrows()
    {
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var result = await _service.GetDateRatesBatchAsync("EUR", new[] { DateTime.UtcNow });

        result.Should().BeEmpty();
    }
}
