using CurrencyConverter.Infrastructure.Auth;
using FluentAssertions;

namespace CurrencyConverter.UnitTests.Infrastructure;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ShouldReturnNonEmptyHash()
    {
        var hash = _hasher.Hash("password123");

        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe("password123");
    }

    [Fact]
    public void Verify_ShouldReturnTrue_ForCorrectPassword()
    {
        var hash = _hasher.Hash("password123");

        var result = _hasher.Verify("password123", hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReturnFalse_ForWrongPassword()
    {
        var hash = _hasher.Hash("password123");

        var result = _hasher.Verify("wrongpassword", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void Hash_ShouldProduceDifferentHashesForSameInput()
    {
        var hash1 = _hasher.Hash("password123");
        var hash2 = _hasher.Hash("password123");

        hash1.Should().NotBe(hash2, "BCrypt uses unique salts");
    }
}
