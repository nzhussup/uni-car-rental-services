using CarRentalService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarRentalService.Data.Repositories;

public class UserRepository(CarRentalDbContext dbContext) : IUserRepository
{
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await dbContext.Users.Include(x => x.Bookings).ToListAsync();
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await dbContext.Users.Include(x => x.Bookings).FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<User> AddAsync(User user)
    {
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    public async Task<User?> UpdateAsync(User user)
    {
        var existingUser = await dbContext.Users.FindAsync(user.Id);
        if (existingUser == null)
        {
            return null;
        }

        dbContext.Entry(existingUser).CurrentValues.SetValues(user);
        await dbContext.SaveChangesAsync();
        return existingUser;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existingUser = await dbContext.Users.FindAsync(id);
        if (existingUser == null) return false;
        dbContext.Users.Remove(existingUser);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}