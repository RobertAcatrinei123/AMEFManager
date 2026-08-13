using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMEFManager.Migrations
{
    /// <inheritdoc />
    public partial class AddPaysTvaToClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Persons_Cnp",
                table: "Persons");

            migrationBuilder.DropIndex(
                name: "IX_Persons_Series_Number",
                table: "Persons");

            migrationBuilder.AddColumn<bool>(
                name: "PaysTVA",
                table: "Clients",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaysTVA",
                table: "Clients");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_Cnp",
                table: "Persons",
                column: "Cnp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Persons_Series_Number",
                table: "Persons",
                columns: new[] { "Series", "Number" },
                unique: true);
        }
    }
}
