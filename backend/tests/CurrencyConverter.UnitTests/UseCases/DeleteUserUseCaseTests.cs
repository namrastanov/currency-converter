using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.UseCases;
using CurrencyConverter.Domain.Constants;
using CurrencyConverter.Domain.Models;
using FluentAssertions;
using Moq;

namespace CurrencyConverter.UnitTests.UseCases;

public class DeleteUserUseCaseTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly DeleteUserUseCase _useCase;

    public DeleteUserUseCaseTests()
    {
        _useCase = new DeleteUserUseCase(_userRepository.Object);
    }

    [Fact]
    public void Execute_ShouldReturnSuccess_WhenUserDeletedSuccessfully()
    {
        var targetId = Guid.NewGuid();
        var currentId = Guid.NewGuid();
        var user = new User { Id = targetId, Username = "user1", PasswordHash = "hash", Role = AppRoles.User };
        _userRepository.Setup(r => r.GetById(targetId)).Returns(user);
        _userRepository.Setup(r => r.Delete(targetId)).Returns(true);

        var result = _useCase.Execute(new DeleteUserCommand(targetId, currentId));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Execute_ShouldReturnFailure_WhenUserNotFound()
    {
        _userRepository.Setup(r => r.GetById(It.IsAny<Guid>())).Returns((User?)null);

        var result = _useCase.Execute(new DeleteUserCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public void Execute_ShouldReturnFailure_WhenDeletingSelf()
    {
        var userId = Guid.NewGuid();

        var result = _useCase.Execute(new DeleteUserCommand(userId, userId));

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("SELF_DELETE");
    }
}
