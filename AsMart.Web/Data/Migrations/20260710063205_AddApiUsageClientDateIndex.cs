using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsMart.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApiUsageClientDateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageLogs_ApiClientId_CreatedAt",
                table: "ApiUsageLogs",
                columns: new[] { "ApiClientId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApiUsageLogs_ApiClientId_CreatedAt",
                table: "ApiUsageLogs");
        }
    }
}
