using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMEFManager.Migrations
{
    /// <inheritdoc />
    public partial class AddC802Document : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "C802Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Number = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C802Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AmefC802Document",
                columns: table => new
                {
                    AmefsId = table.Column<int>(type: "INTEGER", nullable: false),
                    C802DocumentsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmefC802Document", x => new { x.AmefsId, x.C802DocumentsId });
                    table.ForeignKey(
                        name: "FK_AmefC802Document_Amefs_AmefsId",
                        column: x => x.AmefsId,
                        principalTable: "Amefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AmefC802Document_C802Documents_C802DocumentsId",
                        column: x => x.C802DocumentsId,
                        principalTable: "C802Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AmefC802Document_C802DocumentsId",
                table: "AmefC802Document",
                column: "C802DocumentsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AmefC802Document");

            migrationBuilder.DropTable(
                name: "C802Documents");
        }
    }
}
