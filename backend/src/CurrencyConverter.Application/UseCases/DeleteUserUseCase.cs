using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Domain.Constants;
using CurrencyConverter.Domain.Models;

namespace CurrencyConverter.Application.UseCases;

public class DeleteUserUseCase
{
    private readonly IUserRepository _userRepository;

    public DeleteUserUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public virtual Result Execute(DeleteUserCommand command)
    {
        if (command.TargetUserId == command.CurrentUserId)
            return Result.Failure("Cannot delete your own account.", "SELF_DELETE");

        var user = _userRepository.GetById(command.TargetUserId);
        if (user is null)
            return Result.Failure("User not found.", "NOT_FOUND");

        if (string.Equals(user.Username, AppRoles.DefaultAdminUsername, StringComparison.OrdinalIgnoreCase))
            return Result.Failure("The default admin account cannot be deleted.", "DEFAULT_ADMIN");

        _userRepository.Delete(command.TargetUserId);
        return Result.Success();
    }
}
