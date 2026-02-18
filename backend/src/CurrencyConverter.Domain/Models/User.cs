using CurrencyConverter.Domain.Constants;

namespace CurrencyConverter.Domain.Models;

public class User
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public string Role { get; set; } = AppRoles.User;
    public DateTime CreatedAt { get; init; }

    public void ChangeRole(string newRole)
    {
        Role = newRole;
    }
}
