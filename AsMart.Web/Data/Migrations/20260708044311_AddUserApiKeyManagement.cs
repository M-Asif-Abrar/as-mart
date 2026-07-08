using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsMart.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserApiKeyManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ApiClients",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiClients_UserId",
                table: "ApiClients",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiClients_AspNetUsers_UserId",
                table: "ApiClients",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiClients_AspNetUsers_UserId",
                table: "ApiClients");

            migrationBuilder.DropIndex(
                name: "IX_ApiClients_UserId",
                table: "ApiClients");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ApiClients");
        }
    }
}
