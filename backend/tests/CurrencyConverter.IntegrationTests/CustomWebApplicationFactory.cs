using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.Redis;
using WireMock.Server;

namespace CurrencyConverter.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7-alpine")
        .Build();

    public WireMockServer WireMockServer { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        WireMockServer = WireMockServer.Start();
    }

    public new async Task DisposeAsync()
    {
        WireMockServer?.Stop();
        WireMockServer?.Dispose();
        await _redisContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = _redisContainer.GetConnectionString(),
                ["Frankfurter:BaseUrl"] = WireMockServer.Url!,
                ["Frankfurter:TimeoutSeconds"] = "30",
                ["JwtSettings:Secret"] = "TestSecretKeyThatIsAtLeast32CharactersLong!",
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:ExpirationMinutes"] = "60",
                ["RateLimiting:RequestsPerMinute"] = "10000",
                ["CacheSettings:GapMergeThresholdDays"] = "5",
                ["CurrencyProvider:DefaultProvider"] = "Frankfurter"
            });
        });

        builder.UseEnvironment("Testing");
    }
}
