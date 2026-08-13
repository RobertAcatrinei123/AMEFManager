using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMEFManager.Migrations
{
    /// <inheritdoc />
    public partial class removedUniqueAmefNui : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Amefs_NUI",
                table: "Amefs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Amefs_NUI",
                table: "Amefs",
                column: "NUI",
                unique: true);
        }
    }
}
