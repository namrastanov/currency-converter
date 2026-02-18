using System.Text;
using System.Threading.RateLimiting;
using CurrencyConverter.API.Configuration;
using CurrencyConverter.Application.Settings;
using CurrencyConverter.API.HealthChecks;
using CurrencyConverter.Infrastructure.Providers.Frankfurter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RedisRateLimiting;
using StackExchange.Redis;

namespace CurrencyConverter.API.Extensions;

public static class ServiceCollectionExtensions
{
    private const string DefaultJwtSecret = "SuperSecretKeyThatIsAtLeast32CharactersLong!";

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetRequiredSection<JwtSettings>(JwtSettings.SectionName);

        if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret == DefaultJwtSecret)
            throw new InvalidOperationException(
                "JWT Secret is not configured or uses a default value. " +
                "Set a secure secret via environment variables or user secrets.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddRedisRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rateLimitingSettings = configuration.GetRequiredSection<RateLimitingSettings>(RateLimitingSettings.SectionName);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("authenticated", context =>
            {
                var clientId = context.User?.FindFirst("client_id")?.Value
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";

                var redis = context.RequestServices.GetService<IConnectionMultiplexer>();
                if (redis is { IsConnected: true })
                {
                    return RedisRateLimitPartition.GetFixedWindowRateLimiter(clientId, _ => new RedisFixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingSettings.RequestsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        ConnectionMultiplexerFactory = () => redis
                    });
                }

                return RateLimitPartition.GetFixedWindowLimiter(clientId, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitingSettings.RequestsPerMinute,
                    Window = TimeSpan.FromMinutes(1)
                });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too Many Requests",
                    status = 429,
                    detail = "Rate limit exceeded. Please try again later."
                }, cancellationToken);
            };
        });

        return services;
    }

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Currency Converter API", Version = "v1" });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer {token}'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
        });

        return services;
    }

    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsSettings = configuration.GetRequiredSection<CorsSettings>(CorsSettings.SectionName);

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(corsSettings.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    public static IServiceCollection AddApiHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var frankfurterOptions = configuration.GetRequiredSection<FrankfurterOptions>(FrankfurterOptions.SectionName);
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";

        services.AddHttpClient("FrankfurterHealthCheck", client =>
        {
            client.BaseAddress = new Uri(frankfurterOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.AddHealthChecks()
            .AddRedis(redisConnectionString, name: "redis", tags: new[] { "ready" })
            .AddCheck<FrankfurterHealthCheck>("frankfurter", tags: new[] { "ready" });

        return services;
    }
}
