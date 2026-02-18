namespace CurrencyConverter.Application.DTOs;

public record LoginCommand(string Username, string Password);

public record RegisterCommand(string Username, string Password);

public record AuthResult(string Token, string Username, string Role);

public record UserDto(Guid Id, string Username, string Role, DateTime CreatedAt);

public record CreateUserCommand(string Username, string Password, string Role);

public record ChangeRoleCommand(Guid UserId, string NewRole);

public record DeleteUserCommand(Guid TargetUserId, Guid CurrentUserId);
