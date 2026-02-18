using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Domain.Models;

namespace CurrencyConverter.Application.UseCases;

public class LoginUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public virtual async Task<Result<AuthResult>> ExecuteAsync(LoginCommand command)
    {
        var user = _userRepository.GetByUsername(command.Username);
        if (user is null)
            return Result.Failure<AuthResult>("Invalid username or password.", "INVALID_CREDENTIALS");

        var isValid = await Task.Run(() => _passwordHasher.Verify(command.Password, user.PasswordHash));
        if (!isValid)
            return Result.Failure<AuthResult>("Invalid username or password.", "INVALID_CREDENTIALS");

        var token = _jwtTokenService.GenerateToken(user);
        return Result.Success(new AuthResult(token, user.Username, user.Role));
    }
}
