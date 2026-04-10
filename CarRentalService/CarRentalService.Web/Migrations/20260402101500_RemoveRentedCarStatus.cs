using CarRentalService.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalService.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CarRentalDbContext))]
    [Migration("20260402101500_RemoveRentedCarStatus")]
    public partial class RemoveRentedCarStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE dbo.Cars
                SET Status = 0
                WHERE Status = 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE dbo.Cars
                SET Status = 1
                WHERE Status = 2;
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.check_constraints
                    WHERE name = 'CK_Cars_Status_Valid'
                )
                BEGIN
                    ALTER TABLE dbo.Cars
                    ADD CONSTRAINT CK_Cars_Status_Valid CHECK (Status IN (0, 1));
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM sys.check_constraints
                    WHERE name = 'CK_Cars_Status_Valid'
                )
                BEGIN
                    ALTER TABLE dbo.Cars DROP CONSTRAINT CK_Cars_Status_Valid;
                END
                """);

            migrationBuilder.Sql(
                """
                UPDATE dbo.Cars
                SET Status = 2
                WHERE Status = 1;
                """);
        }
    }
}
