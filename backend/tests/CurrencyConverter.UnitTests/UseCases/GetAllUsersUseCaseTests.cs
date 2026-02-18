using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.UseCases;
using CurrencyConverter.Domain.Constants;
using CurrencyConverter.Domain.Models;
using FluentAssertions;
using Moq;

namespace CurrencyConverter.UnitTests.UseCases;

public class GetAllUsersUseCaseTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly GetAllUsersUseCase _useCase;

    public GetAllUsersUseCaseTests()
    {
        _useCase = new GetAllUsersUseCase(_userRepository.Object);
    }

    [Fact]
    public void Execute_ShouldReturnAllUserDtos()
    {
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Username = "admin", PasswordHash = "h1", Role = AppRoles.Admin, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Username = "user1", PasswordHash = "h2", Role = AppRoles.User, CreatedAt = DateTime.UtcNow }
        }.AsReadOnly();
        _userRepository.Setup(r => r.GetAll()).Returns(users);

        var result = _useCase.Execute();

        result.Should().HaveCount(2);
        result[0].Username.Should().Be("admin");
        result[1].Username.Should().Be("user1");
    }
}
