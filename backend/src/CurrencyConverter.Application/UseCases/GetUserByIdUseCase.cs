using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;

namespace CurrencyConverter.Application.UseCases;

public class GetUserByIdUseCase
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public virtual UserDto? Execute(Guid id)
    {
        var user = _userRepository.GetById(id);
        if (user is null)
            return null;

        return new UserDto(user.Id, user.Username, user.Role, user.CreatedAt);
    }
}
