using CarRentalService.Data.Entities;

namespace CarRentalService.Data.Repositories;

public interface ICarRepository
{
    Task<IQueryable<Car>> GetAllAsync();
    Task<Car?> GetByIdAsync(int id);
    Task<Car> AddAsync(Car car);
    Task<Car?> UpdateAsync(Car car);
    Task<bool> DeleteAsync(int id);
    Task SaveChangesAsync();
}
