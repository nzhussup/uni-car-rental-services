using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedBookingIdFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookingId",
                table: "CarUnavailableDates",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookingId",
                table: "CarUnavailableDates");
        }
    }
}
