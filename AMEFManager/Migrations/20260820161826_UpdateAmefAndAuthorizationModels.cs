using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMEFManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAmefAndAuthorizationModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Model",
                table: "Amefs");

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "Authorizations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Configuration",
                table: "Authorizations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "Authorizations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Brand",
                table: "Authorizations");

            migrationBuilder.DropColumn(
                name: "Configuration",
                table: "Authorizations");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "Authorizations");

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Amefs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
