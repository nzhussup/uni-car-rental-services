using CarRentalService.Models;

namespace CarRentalService.Repositories;

public interface ICarRepository
{
    Task<IEnumerable<Car>> GetAllAsync();
    Task<Car?> GetByIdAsync(int id);
    Task<Car> AddAsync(Car car);
    Task<Car?> UpdateAsync(Car car);
    Task<bool> DeleteAsync(int id);
}