using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CurrencyConverter.Application.Settings;
using CurrencyConverter.Domain.Constants;
using CurrencyConverter.Domain.Models;
using CurrencyConverter.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CurrencyConverter.UnitTests.Infrastructure;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _jwtTokenService;
    private readonly JwtSettings _settings;

    public JwtTokenServiceTests()
    {
        _settings = new JwtSettings
        {
            Secret = "TestSecretKeyThatIsAtLeast32CharactersLong!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60
        };
        _jwtTokenService = new JwtTokenService(Options.Create(_settings), TimeProvider.System);
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidJwtToken()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Role = AppRoles.User,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        var token = _jwtTokenService.GenerateToken(user);

        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_ShouldContainCorrectClaims()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Role = AppRoles.Admin,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        var token = _jwtTokenService.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "testuser");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == AppRoles.Admin);
        jwtToken.Claims.Should().Contain(c => c.Type == "client_id" && c.Value == userId.ToString());
        jwtToken.Issuer.Should().Be("TestIssuer");
        jwtToken.Audiences.Should().Contain("TestAudience");
    }

    [Fact]
    public void GenerateToken_ShouldSetCorrectExpiration()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Role = AppRoles.User,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        var before = DateTime.UtcNow;
        var token = _jwtTokenService.GenerateToken(user);
        var after = DateTime.UtcNow;

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.ValidTo.Should().BeAfter(before.AddMinutes(_settings.ExpirationMinutes - 1));
        jwtToken.ValidTo.Should().BeBefore(after.AddMinutes(_settings.ExpirationMinutes + 1));
    }
}
