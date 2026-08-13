using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMEFManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdditionalDocumentToContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdditionalDocuments_Clients_ClientId",
                table: "AdditionalDocuments");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "AdditionalDocuments",
                newName: "ContractId");

            migrationBuilder.RenameIndex(
                name: "IX_AdditionalDocuments_ClientId",
                table: "AdditionalDocuments",
                newName: "IX_AdditionalDocuments_ContractId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdditionalDocuments_Contracts_ContractId",
                table: "AdditionalDocuments",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdditionalDocuments_Contracts_ContractId",
                table: "AdditionalDocuments");

            migrationBuilder.RenameColumn(
                name: "ContractId",
                table: "AdditionalDocuments",
                newName: "ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_AdditionalDocuments_ContractId",
                table: "AdditionalDocuments",
                newName: "IX_AdditionalDocuments_ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdditionalDocuments_Clients_ClientId",
                table: "AdditionalDocuments",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
