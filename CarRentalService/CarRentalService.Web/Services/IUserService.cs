using CarRentalService.Models.DTOs;

namespace CarRentalService.Services;

public interface IUserService
{
    Task<UserDto> GetUserByIdAsync(Guid userId);
}