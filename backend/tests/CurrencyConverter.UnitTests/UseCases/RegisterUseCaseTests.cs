using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.UseCases;
using CurrencyConverter.Domain.Constants;
using CurrencyConverter.Domain.Models;
using FluentAssertions;
using Moq;

namespace CurrencyConverter.UnitTests.UseCases;

public class RegisterUseCaseTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly RegisterUseCase _useCase;

    public RegisterUseCaseTests()
    {
        _useCase = new RegisterUseCase(
            _userRepository.Object,
            _passwordHasher.Object,
            _jwtTokenService.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenRegistrationSucceeds()
    {
        _passwordHasher.Setup(h => h.Hash("password123")).Returns("hashed");
        var createdUser = new User { Id = Guid.NewGuid(), Username = "newuser", PasswordHash = "hashed", Role = AppRoles.User };
        _userRepository.Setup(r => r.TryCreate("newuser", "hashed", AppRoles.User))
            .Returns((true, createdUser));
        _jwtTokenService.Setup(j => j.GenerateToken(createdUser)).Returns("jwt-token");

        var result = await _useCase.ExecuteAsync(new RegisterCommand("newuser", "password123"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("jwt-token");
        result.Value.Username.Should().Be("newuser");
        result.Value.Role.Should().Be(AppRoles.User);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenUsernameExists()
    {
        _passwordHasher.Setup(h => h.Hash("password123")).Returns("hashed");
        var existingUser = new User { Id = Guid.NewGuid(), Username = "existing", PasswordHash = "hash", Role = AppRoles.User };
        _userRepository.Setup(r => r.TryCreate("existing", "hashed", AppRoles.User))
            .Returns((false, existingUser));

        var result = await _useCase.ExecuteAsync(new RegisterCommand("existing", "password123"));

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("USER_ALREADY_EXISTS");
    }
}
