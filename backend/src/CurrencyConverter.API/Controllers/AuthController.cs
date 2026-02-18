using Asp.Versioning;
using CurrencyConverter.API.Models;
using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.UseCases;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[AllowAnonymous]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly LoginUseCase _loginUseCase;
    private readonly RegisterUseCase _registerUseCase;
    private readonly IValidator<LoginCommand> _loginValidator;
    private readonly IValidator<RegisterCommand> _registerValidator;

    public AuthController(
        LoginUseCase loginUseCase,
        RegisterUseCase registerUseCase,
        IValidator<LoginCommand> loginValidator,
        IValidator<RegisterCommand> registerValidator)
    {
        _loginUseCase = loginUseCase;
        _registerUseCase = registerUseCase;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="command">Login credentials.</param>
    /// <returns>A JWT token and user information.</returns>
    /// <response code="200">Returns the JWT token and user details.</response>
    /// <response code="400">Validation error – empty username or password.</response>
    /// <response code="401">Invalid username or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var validationResult = _loginValidator.Validate(command);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var result = await _loginUseCase.ExecuteAsync(command);
        if (result.IsFailure)
            return Unauthorized(ErrorResponse.Create(401, result.Error!, result.ErrorCode));

        return Ok(ApiResponse<AuthResult>.Success(result.Value!));
    }

    /// <summary>
    /// Registers a new user and returns a JWT token.
    /// </summary>
    /// <param name="command">Registration details.</param>
    /// <returns>A JWT token and new user information.</returns>
    /// <response code="201">User created successfully.</response>
    /// <response code="400">Validation error – invalid username or password.</response>
    /// <response code="409">Username already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var validationResult = _registerValidator.Validate(command);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var result = await _registerUseCase.ExecuteAsync(command);
        if (result.IsFailure)
            return Conflict(ErrorResponse.Create(409, result.Error!, result.ErrorCode));

        return CreatedAtAction(nameof(Login), ApiResponse<AuthResult>.Success(result.Value!));
    }
}
