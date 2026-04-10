using CarRentalService.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalService.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CarRentalDbContext))]
    [Migration("20260402113000_SeedInitialCars")]
    public partial class SeedInitialCars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM dbo.Cars)
                BEGIN
                    INSERT INTO dbo.Cars (Make, Model, Year, PriceInUsd, Status)
                    VALUES
                        (N'Toyota', N'Camry', 2023, 56.00, 0),
                        (N'Honda', N'Civic', 2022, 52.00, 0),
                        (N'Ford', N'Focus', 2021, 49.00, 0),
                        (N'BMW', N'3 Series', 2024, 89.00, 0),
                        (N'Mercedes-Benz', N'C-Class', 2023, 94.00, 0),
                        (N'Audi', N'A4', 2022, 86.00, 0),
                        (N'Volkswagen', N'Golf', 2021, 51.00, 0),
                        (N'Hyundai', N'Elantra', 2023, 48.00, 0),
                        (N'Kia', N'K5', 2022, 50.00, 0),
                        (N'Nissan', N'Altima', 2021, 47.00, 0);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left empty. This seed migration is bootstrap data for empty environments.
        }
    }
}
