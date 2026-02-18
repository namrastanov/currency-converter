using CurrencyConverter.API.Configuration;
using CurrencyConverter.API.Models;
using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Settings;
using CurrencyConverter.Domain.Constants;
using CurrencyConverter.Domain.Models;
using FluentAssertions;

namespace CurrencyConverter.UnitTests.API;

public class ConfigurationTests
{
    [Fact]
    public void JwtSettings_ShouldHaveDefaults()
    {
        var settings = new JwtSettings();

        settings.Secret.Should().BeEmpty("secret must be configured explicitly, no hardcoded default");
        settings.Issuer.Should().Be("CurrencyConverter");
        settings.Audience.Should().Be("CurrencyConverterClient");
        settings.ExpirationMinutes.Should().Be(60);
        JwtSettings.SectionName.Should().Be("JwtSettings");
    }

    [Fact]
    public void JwtSettings_ShouldBeConfigurable()
    {
        var settings = new JwtSettings
        {
            Secret = "CustomSecret",
            Issuer = "CustomIssuer",
            Audience = "CustomAudience",
            ExpirationMinutes = 120
        };

        settings.Secret.Should().Be("CustomSecret");
        settings.Issuer.Should().Be("CustomIssuer");
        settings.Audience.Should().Be("CustomAudience");
        settings.ExpirationMinutes.Should().Be(120);
    }

    [Fact]
    public void CorsSettings_ShouldHaveDefaults()
    {
        var settings = new CorsSettings();

        settings.AllowedOrigins.Should().Contain("http://localhost:5173");
        CorsSettings.SectionName.Should().Be("CorsSettings");
    }

    [Fact]
    public void CorsSettings_ShouldBeConfigurable()
    {
        var settings = new CorsSettings
        {
            AllowedOrigins = new[] { "https://example.com" }
        };

        settings.AllowedOrigins.Should().ContainSingle("https://example.com");
    }

    [Fact]
    public void RateLimitingSettings_ShouldHaveDefaults()
    {
        var settings = new RateLimitingSettings();

        settings.RequestsPerMinute.Should().Be(120);
        RateLimitingSettings.SectionName.Should().Be("RateLimiting");
    }

    [Fact]
    public void RateLimitingSettings_ShouldBeConfigurable()
    {
        var settings = new RateLimitingSettings { RequestsPerMinute = 60 };

        settings.RequestsPerMinute.Should().Be(60);
    }

    [Fact]
    public void CacheSettings_ShouldHaveDefaults()
    {
        var settings = new CacheSettings();

        settings.GapMergeThresholdDays.Should().Be(5);
    }

    [Fact]
    public void LoginCommand_ShouldHaveProperties()
    {
        var command = new LoginCommand("user", "pass");

        command.Username.Should().Be("user");
        command.Password.Should().Be("pass");
    }

    [Fact]
    public void RegisterCommand_ShouldHaveProperties()
    {
        var command = new RegisterCommand("user", "pass");

        command.Username.Should().Be("user");
        command.Password.Should().Be("pass");
    }

    [Fact]
    public void AuthResult_ShouldHaveProperties()
    {
        var result = new AuthResult("token", "user", AppRoles.Admin);

        result.Token.Should().Be("token");
        result.Username.Should().Be("user");
        result.Role.Should().Be(AppRoles.Admin);
    }

    [Fact]
    public void ChangeRoleRequest_ShouldHaveProperties()
    {
        var request = new ChangeRoleRequest(AppRoles.Admin);

        request.Role.Should().Be(AppRoles.Admin);
    }

    [Fact]
    public void UserDto_ShouldHaveProperties()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dto = new UserDto(id, "user", AppRoles.Admin, now);

        dto.Id.Should().Be(id);
        dto.Username.Should().Be("user");
        dto.Role.Should().Be(AppRoles.Admin);
        dto.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void User_ShouldHaveProperties()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "test",
            PasswordHash = "hash",
            Role = AppRoles.User,
            CreatedAt = DateTime.UtcNow
        };

        user.Username.Should().Be("test");
        user.PasswordHash.Should().Be("hash");
        user.Role.Should().Be(AppRoles.User);
    }
}
