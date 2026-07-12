using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsMart.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeyLifecycleManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "ApiClients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRotatedAt",
                table: "ApiClients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ApiClients",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "ApiClients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [ApiClients]
                SET [ExpiresAt] = DATEADD(YEAR, 1, SYSUTCDATETIME())
                WHERE [ExpiresAt] IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ApiClients_ExpiresAt",
                table: "ApiClients",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiClients_RevokedAt",
                table: "ApiClients",
                column: "RevokedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApiClients_ExpiresAt",
                table: "ApiClients");

            migrationBuilder.DropIndex(
                name: "IX_ApiClients_RevokedAt",
                table: "ApiClients");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "ApiClients");

            migrationBuilder.DropColumn(
                name: "LastRotatedAt",
                table: "ApiClients");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ApiClients");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "ApiClients");
        }
    }
}