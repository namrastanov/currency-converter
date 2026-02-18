using CurrencyConverter.Domain.Models;

namespace CurrencyConverter.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
