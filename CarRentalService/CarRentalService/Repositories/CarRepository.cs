using CarRentalService.Data;
using CarRentalService.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRentalService.Repositories;

public class CarRepository(CarRentalDbContext context) : ICarRepository
{
    public async Task<IEnumerable<Car>> GetAllAsync()
    {
        return await context.Cars.ToListAsync();
    }

    public async Task<Car?> GetByIdAsync(int id)
    {
        return await context.Cars.FindAsync(id);
    }

    public async Task<Car> AddAsync(Car car)
    {
        context.Cars.Add(car);
        await context.SaveChangesAsync();
        return car;
    }

    public async Task<Car?> UpdateAsync(Car car)
    {
        if (!await context.Cars.AnyAsync(c => c.Id == car.Id))
            return null;

        context.Cars.Update(car);
        await context.SaveChangesAsync();
        return car;
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