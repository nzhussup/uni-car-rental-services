using CarRentalService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarRentalService.Data.Repositories;

public class CarRepository(CarServiceDbContext context) : ICarRepository
{
    public Task<IQueryable<Car>> GetAllAsync()
    {
        // Include bookings so availability filters can consider existing reservations
        return Task.FromResult(context.Cars
            .AsQueryable());
    }

    public async Task<Car?> GetByIdAsync(int id)
    {
        return await context.Cars
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

    public async Task AddUnavailableDateRangeAsync(Car car, int bookingId, DateTime fromDate, DateTime toDate)
    {
        var existingCar = await context.Cars.FindAsync(car.Id);
        if (existingCar == null)
            return;

        existingCar.UnavailableDates.Add(new DateRange()
        {
            BookingId = bookingId,
            PickupDate = fromDate,
            DropOffDate = toDate
        });

        await context.SaveChangesAsync();
    }

    public async Task RemoveUnavailableDateRangeAsync(Car car, int bookingId, DateTime fromDate, DateTime toDate)
    {
        var existingCar = context.Cars.Include(x => x.UnavailableDates).FirstOrDefault(x => x.Id == car.Id);
        if (existingCar == null)
            return;

        existingCar.UnavailableDates.RemoveWhere(b => b.PickupDate.Date == fromDate.Date && b.DropOffDate.Date == toDate.Date && b.BookingId == bookingId);
        await context.SaveChangesAsync();
    }
}
