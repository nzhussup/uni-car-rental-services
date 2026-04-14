using CarRentalService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarRentalService.Data;

public class CarRentalDbContext(DbContextOptions<CarRentalDbContext> options) : DbContext(options)
{
    public DbSet<Car> Cars { get; set; }

    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Car>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.Car).WithMany(x => x.CarBookings).HasForeignKey(e => e.CarId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}