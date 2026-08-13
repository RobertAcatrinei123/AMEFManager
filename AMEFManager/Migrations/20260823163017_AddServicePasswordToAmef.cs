using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMEFManager.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePasswordToAmef : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ServicePassword",
                table: "Amefs",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServicePassword",
                table: "Amefs");
        }
    }
}
