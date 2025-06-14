using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timoto.Migrations
{
    /// <inheritdoc />
    public partial class AddIsMainToCarImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMain",
                table: "CarImages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMain",
                table: "CarImages");
        }
    }
}
