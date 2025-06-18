using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timoto.Migrations
{
    /// <inheritdoc />
    public partial class AddCVVAndExpiredMonthIdAndExpiryYearAndLast4Digits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CVV",
                table: "UserCards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ExpiryMonth",
                table: "UserCards",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExpiryYear",
                table: "UserCards",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CVV",
                table: "UserCards");

            migrationBuilder.DropColumn(
                name: "ExpiryMonth",
                table: "UserCards");

            migrationBuilder.DropColumn(
                name: "ExpiryYear",
                table: "UserCards");
        }
    }
}
