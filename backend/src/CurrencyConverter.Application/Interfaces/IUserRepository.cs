using CurrencyConverter.Domain.Models;

namespace CurrencyConverter.Application.Interfaces;

public interface IUserRepository
{
    User? GetByUsername(string username);
    User? GetById(Guid id);
    IReadOnlyList<User> GetAll();
    User Create(string username, string passwordHash, string role);
    (bool Created, User User) TryCreate(string username, string passwordHash, string role);
    bool UpdateRole(Guid id, string role);
    bool Delete(Guid id);
}
