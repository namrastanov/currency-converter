using System.Security.Claims;
using CurrencyConverter.API.Controllers;
using CurrencyConverter.API.Models;
using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.UseCases;
using CurrencyConverter.Domain.Models;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CurrencyConverter.UnitTests.API;

public class UserManagementControllerTests
{
    private readonly Mock<GetAllUsersUseCase> _getAllUsersUseCase = new(null!);
    private readonly Mock<GetUserByIdUseCase> _getUserByIdUseCase = new(null!);
    private readonly Mock<CreateUserUseCase> _createUserUseCase = new(null!, null!);
    private readonly Mock<UpdateUserRoleUseCase> _updateUserRoleUseCase = new(null!);
    private readonly Mock<DeleteUserUseCase> _deleteUserUseCase = new(null!);
    private readonly Mock<IValidator<CreateUserCommand>> _createUserValidator = new();
    private readonly UserManagementController _controller;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public UserManagementControllerTests()
    {
        _createUserValidator.Setup(v => v.Validate(It.IsAny<CreateUserCommand>()))
            .Returns(new FluentValidation.Results.ValidationResult());

        _controller = new UserManagementController(
            _getAllUsersUseCase.Object,
            _getUserByIdUseCase.Object,
            _createUserUseCase.Object,
            _updateUserRoleUseCase.Object,
            _deleteUserUseCase.Object,
            _createUserValidator.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _currentUserId.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        }));
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public void GetAll_ShouldReturnOk_WithUsers()
    {
        var users = new List<UserDto>
        {
            new(Guid.NewGuid(), "admin", "Admin", DateTime.UtcNow),
            new(Guid.NewGuid(), "user1", "User", DateTime.UtcNow)
        }.AsReadOnly();
        _getAllUsersUseCase.Setup(u => u.Execute()).Returns(users);

        var result = _controller.GetAll();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<IReadOnlyList<UserDto>>>().Subject;
        response.Data.Should().HaveCount(2);
    }

    [Fact]
    public void GetById_ShouldReturnOk_WhenUserExists()
    {
        var userId = Guid.NewGuid();
        var userDto = new UserDto(userId, "testuser", "User", DateTime.UtcNow);
        _getUserByIdUseCase.Setup(u => u.Execute(userId)).Returns(userDto);

        var result = _controller.GetById(userId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        response.Data!.Username.Should().Be("testuser");
    }

    [Fact]
    public void GetById_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        _getUserByIdUseCase.Setup(u => u.Execute(It.IsAny<Guid>())).Returns((UserDto?)null);

        var result = _controller.GetById(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void UpdateRole_ShouldReturnOk_WhenSuccessful()
    {
        var userId = Guid.NewGuid();
        var updatedUser = new UserDto(userId, "user1", "Admin", DateTime.UtcNow);
        _updateUserRoleUseCase.Setup(u => u.Execute(It.IsAny<ChangeRoleCommand>()))
            .Returns(Result.Success(updatedUser));

        var result = _controller.UpdateRole(userId, new ChangeRoleRequest("Admin"));

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        response.Data!.Role.Should().Be("Admin");
    }

    [Fact]
    public void UpdateRole_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        _updateUserRoleUseCase.Setup(u => u.Execute(It.IsAny<ChangeRoleCommand>()))
            .Returns(Result.Failure<UserDto>("User not found.", "NOT_FOUND"));

        var result = _controller.UpdateRole(Guid.NewGuid(), new ChangeRoleRequest("Admin"));

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void UpdateRole_ShouldReturnBadRequest_WhenRoleIsInvalid()
    {
        _updateUserRoleUseCase.Setup(u => u.Execute(It.IsAny<ChangeRoleCommand>()))
            .Returns(Result.Failure<UserDto>("Role must be 'Admin' or 'User'.", "INVALID_ROLE"));

        var result = _controller.UpdateRole(Guid.NewGuid(), new ChangeRoleRequest("SuperAdmin"));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void Delete_ShouldReturnNoContent_WhenSuccessful()
    {
        var userId = Guid.NewGuid();
        _deleteUserUseCase.Setup(u => u.Execute(It.IsAny<DeleteUserCommand>()))
            .Returns(Result.Success());

        var result = _controller.Delete(userId);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void Delete_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        _deleteUserUseCase.Setup(u => u.Execute(It.IsAny<DeleteUserCommand>()))
            .Returns(Result.Failure("User not found.", "NOT_FOUND"));

        var result = _controller.Delete(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void Delete_ShouldReturnBadRequest_WhenDeletingSelf()
    {
        _deleteUserUseCase.Setup(u => u.Execute(It.IsAny<DeleteUserCommand>()))
            .Returns(Result.Failure("Cannot delete your own account.", "SELF_DELETE"));

        var result = _controller.Delete(_currentUserId);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
