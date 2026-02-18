namespace CurrencyConverter.API.Models;

public record CreateUserRequest(string Username, string Password, string Role);

public record ChangeRoleRequest(string Role);
