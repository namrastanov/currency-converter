using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.UseCases;
using CurrencyConverter.Domain.Constants;
using CurrencyConverter.Domain.Models;
using FluentAssertions;
using Moq;

namespace CurrencyConverter.UnitTests.UseCases;

public class LoginUseCaseTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly LoginUseCase _useCase;

    public LoginUseCaseTests()
    {
        _useCase = new LoginUseCase(
            _userRepository.Object,
            _passwordHasher.Object,
            _jwtTokenService.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenCredentialsAreValid()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "testuser", PasswordHash = "hash", Role = AppRoles.User };
        _userRepository.Setup(r => r.GetByUsername("testuser")).Returns(user);
        _passwordHasher.Setup(h => h.Verify("password", "hash")).Returns(true);
        _jwtTokenService.Setup(j => j.GenerateToken(user)).Returns("jwt-token");

        var result = await _useCase.ExecuteAsync(new LoginCommand("testuser", "password"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("jwt-token");
        result.Value.Username.Should().Be("testuser");
        result.Value.Role.Should().Be(AppRoles.User);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenUserNotFound()
    {
        _userRepository.Setup(r => r.GetByUsername("unknown")).Returns((User?)null);

        var result = await _useCase.ExecuteAsync(new LoginCommand("unknown", "password"));

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenPasswordIsWrong()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "testuser", PasswordHash = "hash", Role = AppRoles.User };
        _userRepository.Setup(r => r.GetByUsername("testuser")).Returns(user);
        _passwordHasher.Setup(h => h.Verify("wrong", "hash")).Returns(false);

        var result = await _useCase.ExecuteAsync(new LoginCommand("testuser", "wrong"));

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }
}
