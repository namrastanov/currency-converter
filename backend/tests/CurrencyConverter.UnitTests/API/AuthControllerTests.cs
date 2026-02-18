using CurrencyConverter.API.Controllers;
using CurrencyConverter.API.Models;
using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.UseCases;
using CurrencyConverter.Domain.Constants;
using CurrencyConverter.Domain.Models;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CurrencyConverter.UnitTests.API;

public class AuthControllerTests
{
    private readonly Mock<LoginUseCase> _loginUseCase = new(null!, null!, null!);
    private readonly Mock<RegisterUseCase> _registerUseCase = new(null!, null!, null!);
    private readonly Mock<IValidator<LoginCommand>> _loginValidator = new();
    private readonly Mock<IValidator<RegisterCommand>> _registerValidator = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _loginValidator.Setup(v => v.Validate(It.IsAny<LoginCommand>()))
            .Returns(new ValidationResult());
        _registerValidator.Setup(v => v.Validate(It.IsAny<RegisterCommand>()))
            .Returns(new ValidationResult());

        _controller = new AuthController(
            _loginUseCase.Object,
            _registerUseCase.Object,
            _loginValidator.Object,
            _registerValidator.Object);
    }

    [Fact]
    public async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
    {
        var command = new LoginCommand("user", "pass");
        var authResult = new AuthResult("jwt-token", "user", AppRoles.User);
        _loginUseCase.Setup(u => u.ExecuteAsync(command))
            .ReturnsAsync(Result.Success(authResult));

        var result = await _controller.Login(command);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<AuthResult>>().Subject;
        response.Data!.Token.Should().Be("jwt-token");
        response.Data.Username.Should().Be("user");
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
    {
        var command = new LoginCommand("unknown", "pass");
        _loginUseCase.Setup(u => u.ExecuteAsync(command))
            .ReturnsAsync(Result.Failure<AuthResult>("Invalid username or password.", "INVALID_CREDENTIALS"));

        var result = await _controller.Login(command);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_ShouldThrowValidationException_WhenInputIsInvalid()
    {
        var command = new LoginCommand("", "");
        _loginValidator.Setup(v => v.Validate(command))
            .Returns(new ValidationResult(new[] { new ValidationFailure("Username", "Username is required.") }));

        await _controller.Invoking(c => c.Login(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Register_ShouldReturnCreated_WhenSuccessful()
    {
        var command = new RegisterCommand("newuser", "password123");
        var authResult = new AuthResult("jwt-token", "newuser", AppRoles.User);
        _registerUseCase.Setup(u => u.ExecuteAsync(command))
            .ReturnsAsync(Result.Success(authResult));

        var result = await _controller.Register(command);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Register_ShouldReturnConflict_WhenUsernameExists()
    {
        var command = new RegisterCommand("existing", "password123");
        _registerUseCase.Setup(u => u.ExecuteAsync(command))
            .ReturnsAsync(Result.Failure<AuthResult>("User 'existing' already exists.", "USER_ALREADY_EXISTS"));

        var result = await _controller.Register(command);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Register_ShouldThrowValidationException_WhenInputIsInvalid()
    {
        var command = new RegisterCommand("user", "12345");
        _registerValidator.Setup(v => v.Validate(command))
            .Returns(new ValidationResult(new[] { new ValidationFailure("Password", "Password must be at least 6 characters.") }));

        await _controller.Invoking(c => c.Register(command))
            .Should().ThrowAsync<ValidationException>();
    }
}
