using BookingService.Models.DTOs;

namespace BookingService.Services;

public interface IUserService
{
    Task<UserDto> GetUserByIdAsync(Guid userId);
}