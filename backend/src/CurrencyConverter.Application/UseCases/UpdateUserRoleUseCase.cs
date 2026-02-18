using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Domain.Constants;
using CurrencyConverter.Domain.Models;

namespace CurrencyConverter.Application.UseCases;

public class UpdateUserRoleUseCase
{
    private readonly IUserRepository _userRepository;

    public UpdateUserRoleUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public virtual Result<UserDto> Execute(ChangeRoleCommand command)
    {
        if (command.NewRole != AppRoles.Admin && command.NewRole != AppRoles.User)
            return Result.Failure<UserDto>($"Role must be '{AppRoles.Admin}' or '{AppRoles.User}'.", "INVALID_ROLE");

        var user = _userRepository.GetById(command.UserId);
        if (user is null)
            return Result.Failure<UserDto>("User not found.", "NOT_FOUND");

        if (string.Equals(user.Username, AppRoles.DefaultAdminUsername, StringComparison.OrdinalIgnoreCase))
            return Result.Failure<UserDto>("The default admin account role cannot be changed.", "DEFAULT_ADMIN");

        _userRepository.UpdateRole(command.UserId, command.NewRole);
        return Result.Success(new UserDto(user.Id, user.Username, command.NewRole, user.CreatedAt));
    }
}
