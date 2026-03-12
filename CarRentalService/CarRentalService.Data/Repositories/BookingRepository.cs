using CarRentalService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarRentalService.Data.Repositories;

public class BookingRepository(CarRentalDbContext context) : IBookingRepository
{
    public async Task<IEnumerable<Booking>> GetAllAsync()
    {
        return await context.Bookings
            .Include(x => x.Car)
            .Include(x => x.User)
            .ToListAsync();
    }

    public async Task<Booking?> GetByIdAsync(int id)
    {
        return await context.Bookings
            .Include(x => x.Car)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Booking> AddAsync(Booking booking)
    {
        await context.Bookings.AddAsync(booking);
        await context.SaveChangesAsync();
        return booking;
    }

    public async Task<Booking?> UpdateAsync(Booking booking)
    {
        var existingBooking = await context.Bookings.FindAsync(booking.Id);
        if (existingBooking == null)
        {
            return null;
        }

        context.Entry(existingBooking).CurrentValues.SetValues(booking);
        await context.SaveChangesAsync();
        return existingBooking;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var booking = await context.Bookings
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (booking is null) return false;

        context.Bookings.Remove(booking);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}