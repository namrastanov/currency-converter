using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.UseCases;
using CurrencyConverter.Domain.Constants;
using CurrencyConverter.Domain.Models;
using FluentAssertions;
using Moq;

namespace CurrencyConverter.UnitTests.UseCases;

public class UpdateUserRoleUseCaseTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly UpdateUserRoleUseCase _useCase;

    public UpdateUserRoleUseCaseTests()
    {
        _useCase = new UpdateUserRoleUseCase(_userRepository.Object);
    }

    [Fact]
    public void Execute_ShouldReturnSuccess_WhenSuccessful()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "user1", PasswordHash = "hash", Role = AppRoles.User, CreatedAt = DateTime.UtcNow };
        _userRepository.Setup(r => r.GetById(userId)).Returns(user);
        _userRepository.Setup(r => r.UpdateRole(userId, AppRoles.Admin)).Returns(true);

        var result = _useCase.Execute(new ChangeRoleCommand(userId, AppRoles.Admin));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be(AppRoles.Admin);
    }

    [Fact]
    public void Execute_ShouldReturnFailure_WhenUserNotFound()
    {
        _userRepository.Setup(r => r.GetById(It.IsAny<Guid>())).Returns((User?)null);

        var result = _useCase.Execute(new ChangeRoleCommand(Guid.NewGuid(), AppRoles.Admin));

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public void Execute_ShouldReturnFailure_WhenRoleIsInvalid()
    {
        var result = _useCase.Execute(new ChangeRoleCommand(Guid.NewGuid(), "SuperAdmin"));

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("INVALID_ROLE");
    }
}
