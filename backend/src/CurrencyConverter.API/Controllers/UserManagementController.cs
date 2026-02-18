using System.Security.Claims;
using Asp.Versioning;
using CurrencyConverter.API.Models;
using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.UseCases;
using CurrencyConverter.Domain.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.API.Controllers;

/// <summary>
/// Admin-only endpoints for managing users.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/admin/users")]
[Authorize(Roles = AppRoles.Admin)]
[Produces("application/json")]
public class UserManagementController : ControllerBase
{
    private readonly GetAllUsersUseCase _getAllUsersUseCase;
    private readonly GetUserByIdUseCase _getUserByIdUseCase;
    private readonly CreateUserUseCase _createUserUseCase;
    private readonly UpdateUserRoleUseCase _updateUserRoleUseCase;
    private readonly DeleteUserUseCase _deleteUserUseCase;
    private readonly IValidator<CreateUserCommand> _createUserValidator;

    public UserManagementController(
        GetAllUsersUseCase getAllUsersUseCase,
        GetUserByIdUseCase getUserByIdUseCase,
        CreateUserUseCase createUserUseCase,
        UpdateUserRoleUseCase updateUserRoleUseCase,
        DeleteUserUseCase deleteUserUseCase,
        IValidator<CreateUserCommand> createUserValidator)
    {
        _getAllUsersUseCase = getAllUsersUseCase;
        _getUserByIdUseCase = getUserByIdUseCase;
        _createUserUseCase = createUserUseCase;
        _updateUserRoleUseCase = updateUserRoleUseCase;
        _deleteUserUseCase = deleteUserUseCase;
        _createUserValidator = createUserValidator;
    }

    /// <summary>
    /// Gets all registered users.
    /// </summary>
    /// <returns>A list of all users.</returns>
    /// <response code="200">Returns the list of users.</response>
    /// <response code="401">Unauthorized – missing or invalid JWT token.</response>
    /// <response code="403">Forbidden – user does not have Admin role.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetAll()
    {
        var users = _getAllUsersUseCase.Execute();
        return Ok(ApiResponse<IReadOnlyList<UserDto>>.Success(users));
    }

    /// <summary>
    /// Gets a user by their unique identifier.
    /// </summary>
    /// <param name="id">The user's GUID.</param>
    /// <returns>The user details.</returns>
    /// <response code="200">Returns the user.</response>
    /// <response code="401">Unauthorized – missing or invalid JWT token.</response>
    /// <response code="403">Forbidden – user does not have Admin role.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id)
    {
        var user = _getUserByIdUseCase.Execute(id);
        if (user is null)
            return NotFound(ErrorResponse.NotFound("User not found."));

        return Ok(ApiResponse<UserDto>.Success(user));
    }

    /// <summary>
    /// Creates a new user with the specified role.
    /// </summary>
    /// <param name="request">Username, password, and role for the new user.</param>
    /// <returns>The created user details.</returns>
    /// <response code="201">User created successfully.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Unauthorized – missing or invalid JWT token.</response>
    /// <response code="403">Forbidden – user does not have Admin role.</response>
    /// <response code="409">Username already exists.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public IActionResult Create([FromBody] CreateUserRequest request)
    {
        var command = new CreateUserCommand(request.Username, request.Password, request.Role);
        var validationResult = _createUserValidator.Validate(command);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var result = _createUserUseCase.Execute(command);
        if (result.IsFailure)
        {
            if (result.ErrorCode == "USER_ALREADY_EXISTS")
                return Conflict(ErrorResponse.Create(409, result.Error!, result.ErrorCode));
            return BadRequest(ErrorResponse.BadRequest(result.Error!));
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, ApiResponse<UserDto>.Success(result.Value));
    }

    /// <summary>
    /// Updates a user's role.
    /// </summary>
    /// <param name="id">The user's GUID.</param>
    /// <param name="request">The new role to assign (must be "Admin" or "User").</param>
    /// <returns>The updated user details.</returns>
    /// <response code="200">Role updated successfully.</response>
    /// <response code="400">Invalid role value.</response>
    /// <response code="401">Unauthorized – missing or invalid JWT token.</response>
    /// <response code="403">Forbidden – user does not have Admin role.</response>
    /// <response code="404">User not found.</response>
    [HttpPut("{id:guid}/role")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult UpdateRole(Guid id, [FromBody] ChangeRoleRequest request)
    {
        var command = new ChangeRoleCommand(id, request.Role);
        var result = _updateUserRoleUseCase.Execute(command);
        if (result.IsFailure)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(ErrorResponse.NotFound(result.Error!));
            return BadRequest(ErrorResponse.BadRequest(result.Error!));
        }

        return Ok(ApiResponse<UserDto>.Success(result.Value!));
    }

    /// <summary>
    /// Deletes a user by their unique identifier.
    /// </summary>
    /// <param name="id">The user's GUID.</param>
    /// <response code="204">User deleted successfully.</response>
    /// <response code="400">Cannot delete your own account.</response>
    /// <response code="401">Unauthorized – missing or invalid JWT token.</response>
    /// <response code="403">Forbidden – user does not have Admin role.</response>
    /// <response code="404">User not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id)
    {
        var nameIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var currentUserId))
            return Unauthorized();

        var command = new DeleteUserCommand(id, currentUserId);
        var result = _deleteUserUseCase.Execute(command);

        if (result.IsFailure)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(ErrorResponse.NotFound(result.Error!));
            return BadRequest(ErrorResponse.BadRequest(result.Error!));
        }

        return NoContent();
    }
}
