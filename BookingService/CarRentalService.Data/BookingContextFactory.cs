using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarRentalService.Data;

public class BookingContextFactory : IDesignTimeDbContextFactory<BookingDbContext>
{
    public BookingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BookingDbContext>();

        // This is only used by the CLI tools (dotnet ef) to scaffold migrations.
        // Use your Docker connection string here.
        optionsBuilder.UseSqlServer("Server=localhost,1433;Database=BookingDB;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;");

        return new BookingDbContext(optionsBuilder.Options);
    }
}