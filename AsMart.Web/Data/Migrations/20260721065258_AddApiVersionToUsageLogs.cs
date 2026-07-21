using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsMart.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApiVersionToUsageLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiVersion",
                table: "ApiUsageLogs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "legacy");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageLogs_ApiVersion",
                table: "ApiUsageLogs",
                column: "ApiVersion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApiUsageLogs_ApiVersion",
                table: "ApiUsageLogs");

            migrationBuilder.DropColumn(
                name: "ApiVersion",
                table: "ApiUsageLogs");
        }
    }
}
