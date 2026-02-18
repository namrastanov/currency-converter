using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;

namespace CurrencyConverter.Application.UseCases;

public class GetAllUsersUseCase
{
    private readonly IUserRepository _userRepository;

    public GetAllUsersUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public virtual IReadOnlyList<UserDto> Execute()
    {
        return _userRepository.GetAll()
            .Select(u => new UserDto(u.Id, u.Username, u.Role, u.CreatedAt))
            .ToList()
            .AsReadOnly();
    }
}
