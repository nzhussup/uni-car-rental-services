using CarRentalService.Data.Entities;

namespace CarRentalService.Data.Repositories;

public interface ICarRepository
{
    Task<IQueryable<Car>> GetAllAsync();
    Task<Car?> GetByIdAsync(int id);
    Task<Car> AddAsync(Car car);
    Task<Car?> UpdateAsync(Car car);
    Task<bool> DeleteAsync(int id);
    Task AddUnavailableDateRangeAsync(Car car, int bookingId, DateTime fromDate, DateTime toDate);
    Task RemoveUnavailableDateRangeAsync(Car car, int bookingId, DateTime fromDate, DateTime toDate);
    Task SaveChangesAsync();
}
