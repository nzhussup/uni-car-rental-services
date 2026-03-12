using CarRentalService.Data.Entities;

namespace CarRentalService.Data.Repositories;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
    Task<User> AddAsync(User user);
    Task<User?> UpdateAsync(User user);
    Task<bool> DeleteAsync(int id);
    Task SaveChangesAsync();
}