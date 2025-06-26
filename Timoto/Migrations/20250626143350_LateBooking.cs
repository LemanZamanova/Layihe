using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timoto.Migrations
{
    /// <inheritdoc />
    public partial class LateBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LatePenaltyAmount",
                table: "Bookings",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatePenaltyAmount",
                table: "Bookings");
        }
    }
}
