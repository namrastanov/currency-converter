using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Domain.Constants;
using CurrencyConverter.Domain.Models;

namespace CurrencyConverter.Application.UseCases;

public class CreateUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public virtual Result<UserDto> Execute(CreateUserCommand command)
    {
        if (command.Role != AppRoles.Admin && command.Role != AppRoles.User)
            return Result.Failure<UserDto>($"Role must be '{AppRoles.Admin}' or '{AppRoles.User}'.", "INVALID_ROLE");

        var passwordHash = _passwordHasher.Hash(command.Password);

        var (created, user) = _userRepository.TryCreate(command.Username, passwordHash, command.Role);
        if (!created)
            return Result.Failure<UserDto>($"User '{command.Username}' already exists.", "USER_ALREADY_EXISTS");

        return Result.Success(new UserDto(user.Id, user.Username, user.Role, user.CreatedAt));
    }
}
