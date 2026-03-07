using CarRentalService.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRentalService.Data;

public class CarRentalDbContext(DbContextOptions<CarRentalDbContext> options) : DbContext(options)
{
    public DbSet<Car> Cars { get; set; }
}