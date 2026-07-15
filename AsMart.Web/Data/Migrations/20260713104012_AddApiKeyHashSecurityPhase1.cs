using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsMart.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeyHashSecurityPhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApiClients_ApiKey",
                table: "ApiClients");

            migrationBuilder.AlterColumn<string>(
                name: "ApiKey",
                table: "ApiClients",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "ApiKeyHash",
                table: "ApiClients",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiKeyPrefix",
                table: "ApiClients",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiClients_ApiKey",
                table: "ApiClients",
                column: "ApiKey",
                unique: true,
                filter: "[ApiKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApiClients_ApiKeyHash",
                table: "ApiClients",
                column: "ApiKeyHash",
                unique: true,
                filter: "[ApiKeyHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApiClients_ApiKeyPrefix",
                table: "ApiClients",
                column: "ApiKeyPrefix");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApiClients_ApiKey",
                table: "ApiClients");

            migrationBuilder.DropIndex(
                name: "IX_ApiClients_ApiKeyHash",
                table: "ApiClients");

            migrationBuilder.DropIndex(
                name: "IX_ApiClients_ApiKeyPrefix",
                table: "ApiClients");

            migrationBuilder.DropColumn(
                name: "ApiKeyHash",
                table: "ApiClients");

            migrationBuilder.DropColumn(
                name: "ApiKeyPrefix",
                table: "ApiClients");

            migrationBuilder.AlterColumn<string>(
                name: "ApiKey",
                table: "ApiClients",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiClients_ApiKey",
                table: "ApiClients",
                column: "ApiKey",
                unique: true);
        }
    }
}
