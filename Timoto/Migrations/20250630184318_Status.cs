using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timoto.Migrations
{
    /// <inheritdoc />
    public partial class Status : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Cars",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Cars",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cars_UserId",
                table: "Cars",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cars_AspNetUsers_UserId",
                table: "Cars",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cars_AspNetUsers_UserId",
                table: "Cars");

            migrationBuilder.DropIndex(
                name: "IX_Cars_UserId",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Cars");
        }
    }
}
