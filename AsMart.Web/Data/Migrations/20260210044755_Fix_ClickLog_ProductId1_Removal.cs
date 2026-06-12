using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsMart.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Fix_ClickLog_ProductId1_Removal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ClickLogs_Products_ProductId1')
BEGIN
    ALTER TABLE [ClickLogs] DROP CONSTRAINT [FK_ClickLogs_Products_ProductId1];
END

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClickLogs_ProductId1' AND object_id = OBJECT_ID('[ClickLogs]'))
BEGIN
    DROP INDEX [IX_ClickLogs_ProductId1] ON [ClickLogs];
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE name = 'ProductId1' AND object_id = OBJECT_ID('[ClickLogs]'))
BEGIN
    ALTER TABLE [ClickLogs] DROP COLUMN [ProductId1];
END
");

            migrationBuilder.DropForeignKey(
                name: "FK_ClickLogs_BlogPosts_BlogPostId",
                table: "ClickLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_ClickLogs_BlogPosts_BlogPostId",
                table: "ClickLogs",
                column: "BlogPostId",
                principalTable: "BlogPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }



        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClickLogs_BlogPosts_BlogPostId",
                table: "ClickLogs");

            migrationBuilder.AddColumn<int>(
                name: "ProductId1",
                table: "ClickLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_ProductId1",
                table: "ClickLogs",
                column: "ProductId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ClickLogs_Products_ProductId1",
                table: "ClickLogs",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClickLogs_BlogPosts_BlogPostId",
                table: "ClickLogs",
                column: "BlogPostId",
                principalTable: "BlogPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

    }
}
