using CarRentalService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarRentalService.Data.Repositories;

public class CarRepository(CarRentalDbContext context) : ICarRepository
{
    public Task<IQueryable<Car>> GetAllAsync()
    {
        // Include bookings so availability filters can consider existing reservations
        return Task.FromResult(context.Cars
            .Include(c => c.CarBookings)
            .AsQueryable());
    }

    public async Task<Car?> GetByIdAsync(int id)
    {
        return await context.Cars
            .Include(c => c.CarBookings)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Car> AddAsync(Car car)
    {
        context.Cars.Add(car);
        await context.SaveChangesAsync();
        return car;
    }

    public async Task<Car?> UpdateAsync(Car car)
    {
        var existingCar = await context.Cars.FindAsync(car.Id);
        if (existingCar == null)
            return null;

        context.Entry(existingCar).CurrentValues.SetValues(car);
        await context.SaveChangesAsync();
        return existingCar;
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var car = await context.Cars.FindAsync(id);
        if (car is null) return false;

        context.Cars.Remove(car);
        await context.SaveChangesAsync();
        return true;
    }
}
