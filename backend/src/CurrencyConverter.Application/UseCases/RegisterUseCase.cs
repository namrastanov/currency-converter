using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Domain.Constants;
using CurrencyConverter.Domain.Models;

namespace CurrencyConverter.Application.UseCases;

public class RegisterUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public RegisterUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public virtual async Task<Result<AuthResult>> ExecuteAsync(RegisterCommand command)
    {
        var passwordHash = await Task.Run(() => _passwordHasher.Hash(command.Password));

        var (created, user) = _userRepository.TryCreate(command.Username, passwordHash, AppRoles.User);
        if (!created)
            return Result.Failure<AuthResult>($"User '{command.Username}' already exists.", "USER_ALREADY_EXISTS");

        var token = _jwtTokenService.GenerateToken(user);
        return Result.Success(new AuthResult(token, user.Username, user.Role));
    }
}
