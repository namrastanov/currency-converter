using System.Diagnostics.CodeAnalysis;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Settings;
using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.Infrastructure.Auth;
using CurrencyConverter.Infrastructure.Caching;
using CurrencyConverter.Infrastructure.Http;
using CurrencyConverter.Infrastructure.Providers;
using CurrencyConverter.Infrastructure.Providers.Frankfurter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using StackExchange.Redis;

namespace CurrencyConverter.Infrastructure;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FrankfurterOptions>(configuration.GetSection(FrankfurterOptions.SectionName));
        services.Configure<CacheSettings>(configuration.GetSection(CacheSettings.SectionName));
        services.Configure<CurrencyProviderSettings>(configuration.GetSection(CurrencyProviderSettings.SectionName));

        var frankfurterOptions = new FrankfurterOptions();
        configuration.GetSection(FrankfurterOptions.SectionName).Bind(frankfurterOptions);

        services.AddTransient<CorrelationIdDelegatingHandler>();

        services.AddHttpClient<ICurrencyProvider, FrankfurterProvider>(client =>
        {
            client.BaseAddress = new Uri(frankfurterOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(frankfurterOptions.TimeoutSeconds);
        })
        .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
        .AddResilienceHandler("frankfurter-pipeline", builder =>
        {
            builder.AddTimeout(TimeSpan.FromSeconds(45));

            builder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(200)
            });

            builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30)
            });

            builder.AddTimeout(TimeSpan.FromSeconds(10));
        });

        services.AddTransient<ICurrencyProviderFactory, CurrencyProviderFactory>();

        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var config = ConfigurationOptions.Parse(redisConnectionString);
            config.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(config);
        });

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "CurrencyConverter:";
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }

    public static IServiceCollection AddInMemoryUserRepository(this IServiceCollection services)
    {
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        return services;
    }
}
