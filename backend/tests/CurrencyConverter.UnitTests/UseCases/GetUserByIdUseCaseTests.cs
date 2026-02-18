using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.UseCases;
using CurrencyConverter.Domain.Constants;
using CurrencyConverter.Domain.Models;
using FluentAssertions;
using Moq;

namespace CurrencyConverter.UnitTests.UseCases;

public class GetUserByIdUseCaseTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly GetUserByIdUseCase _useCase;

    public GetUserByIdUseCaseTests()
    {
        _useCase = new GetUserByIdUseCase(_userRepository.Object);
    }

    [Fact]
    public void Execute_ShouldReturnUserDto_WhenUserExists()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "testuser", PasswordHash = "hash", Role = AppRoles.User, CreatedAt = DateTime.UtcNow };
        _userRepository.Setup(r => r.GetById(userId)).Returns(user);

        var result = _useCase.Execute(userId);

        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
        result.Id.Should().Be(userId);
    }

    [Fact]
    public void Execute_ShouldReturnNull_WhenUserNotFound()
    {
        _userRepository.Setup(r => r.GetById(It.IsAny<Guid>())).Returns((User?)null);

        var result = _useCase.Execute(Guid.NewGuid());

        result.Should().BeNull();
    }
}
