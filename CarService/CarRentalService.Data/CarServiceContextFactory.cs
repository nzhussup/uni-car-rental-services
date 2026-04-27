using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarRentalService.Data;

public class CarServiceContextFactory : IDesignTimeDbContextFactory<CarServiceDbContext>
{
    public CarServiceDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CarServiceDbContext>();

        // This is only used by the CLI tools (dotnet ef) to scaffold migrations.
        // Use your Docker connection string here.
        optionsBuilder.UseSqlServer("Server=localhost,1433;Database=CarDB;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;");

        return new CarServiceDbContext(optionsBuilder.Options);
    }
}