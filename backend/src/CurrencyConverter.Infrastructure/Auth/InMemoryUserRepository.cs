using System.Collections.Concurrent;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Domain.Constants;
using CurrencyConverter.Domain.Models;

namespace CurrencyConverter.Infrastructure.Auth;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();
    private readonly TimeProvider _timeProvider;
    private readonly object _createLock = new();

    public InMemoryUserRepository(IPasswordHasher passwordHasher, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        var adminId = Guid.NewGuid();
        _users.TryAdd(adminId, new User
        {
            Id = adminId,
            Username = "admin",
            PasswordHash = passwordHasher.Hash("admin123"),
            Role = AppRoles.Admin,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        });
    }

    public User? GetByUsername(string username)
    {
        return _users.Values.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public User? GetById(Guid id)
    {
        _users.TryGetValue(id, out var user);
        return user;
    }

    public IReadOnlyList<User> GetAll()
    {
        return _users.Values.OrderBy(u => u.CreatedAt).ToList().AsReadOnly();
    }

    public User Create(string username, string passwordHash, string role)
    {
        var (_, user) = TryCreate(username, passwordHash, role);
        return user;
    }

    public (bool Created, User User) TryCreate(string username, string passwordHash, string role)
    {
        lock (_createLock)
        {
            var existing = GetByUsername(username);
            if (existing is not null)
                return (false, existing);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordHash = passwordHash,
                Role = role,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
            };

            _users.TryAdd(user.Id, user);
            return (true, user);
        }
    }

    public bool UpdateRole(Guid id, string role)
    {
        if (!_users.TryGetValue(id, out var user))
            return false;

        user.ChangeRole(role);
        return true;
    }

    public bool Delete(Guid id)
    {
        return _users.TryRemove(id, out _);
    }
}
