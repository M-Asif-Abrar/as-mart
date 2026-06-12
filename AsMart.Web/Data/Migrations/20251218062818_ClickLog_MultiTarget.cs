using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsMart.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClickLog_MultiTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClickLogs_Products_ProductId",
                table: "ClickLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClickLogs_ProductId",
                table: "ClickLogs");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "ClickLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "BlogPostId",
                table: "ClickLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClickType",
                table: "ClickLogs",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProductId1",
                table: "ClickLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_BlogPostId_ClickType_ClickedAt",
                table: "ClickLogs",
                columns: new[] { "BlogPostId", "ClickType", "ClickedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_ClickedAt",
                table: "ClickLogs",
                column: "ClickedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_ProductId_ClickType_ClickedAt",
                table: "ClickLogs",
                columns: new[] { "ProductId", "ClickType", "ClickedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_ProductId1",
                table: "ClickLogs",
                column: "ProductId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ClickLogs_BlogPosts_BlogPostId",
                table: "ClickLogs",
                column: "BlogPostId",
                principalTable: "BlogPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClickLogs_Products_ProductId",
                table: "ClickLogs",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClickLogs_Products_ProductId1",
                table: "ClickLogs",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClickLogs_BlogPosts_BlogPostId",
                table: "ClickLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ClickLogs_Products_ProductId",
                table: "ClickLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ClickLogs_Products_ProductId1",
                table: "ClickLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClickLogs_BlogPostId_ClickType_ClickedAt",
                table: "ClickLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClickLogs_ClickedAt",
                table: "ClickLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClickLogs_ProductId_ClickType_ClickedAt",
                table: "ClickLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClickLogs_ProductId1",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "BlogPostId",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "ClickType",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "ClickLogs");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "ClickLogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_ProductId",
                table: "ClickLogs",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClickLogs_Products_ProductId",
                table: "ClickLogs",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
