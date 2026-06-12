using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsMart.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialUtmTrackingToClickLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClickLogs_BlogPosts_BlogPostId",
                table: "ClickLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ClickLogs_Products_ProductId",
                table: "ClickLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_MarketingPostingQueue_MarketingCaptionVariations_MarketingCaptionVariationId",
                table: "MarketingPostingQueue");

            migrationBuilder.AlterColumn<string>(
                name: "ClickType",
                table: "ClickLogs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AddColumn<bool>(
                name: "IsFacebookTraffic",
                table: "ClickLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsInstagramTraffic",
                table: "ClickLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinterestTraffic",
                table: "ClickLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSocialTraffic",
                table: "ClickLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTelegramTraffic",
                table: "ClickLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LandingUrl",
                table: "ClickLogs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarketingCampaignId",
                table: "ClickLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferrerUrl",
                table: "ClickLogs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SocialTargetId",
                table: "ClickLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmCampaign",
                table: "ClickLogs",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmContent",
                table: "ClickLogs",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmMedium",
                table: "ClickLogs",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmSource",
                table: "ClickLogs",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmTerm",
                table: "ClickLogs",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_IsFacebookTraffic",
                table: "ClickLogs",
                column: "IsFacebookTraffic");

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_IsSocialTraffic",
                table: "ClickLogs",
                column: "IsSocialTraffic");

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_MarketingCampaignId_ClickType_ClickedAt",
                table: "ClickLogs",
                columns: new[] { "MarketingCampaignId", "ClickType", "ClickedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_SocialTargetId_ClickType_ClickedAt",
                table: "ClickLogs",
                columns: new[] { "SocialTargetId", "ClickType", "ClickedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_UtmCampaign",
                table: "ClickLogs",
                column: "UtmCampaign");

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_UtmContent",
                table: "ClickLogs",
                column: "UtmContent");

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_UtmMedium",
                table: "ClickLogs",
                column: "UtmMedium");

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_UtmSource",
                table: "ClickLogs",
                column: "UtmSource");

            migrationBuilder.AddForeignKey(
                name: "FK_ClickLogs_BlogPosts_BlogPostId",
                table: "ClickLogs",
                column: "BlogPostId",
                principalTable: "BlogPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ClickLogs_MarketingCampaigns_MarketingCampaignId",
                table: "ClickLogs",
                column: "MarketingCampaignId",
                principalTable: "MarketingCampaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ClickLogs_Products_ProductId",
                table: "ClickLogs",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ClickLogs_SocialTargets_SocialTargetId",
                table: "ClickLogs",
                column: "SocialTargetId",
                principalTable: "SocialTargets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MarketingPostingQueue_MarketingCaptionVariations_MarketingCaptionVariationId",
                table: "MarketingPostingQueue",
                column: "MarketingCaptionVariationId",
                principalTable: "MarketingCaptionVariations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClickLogs_BlogPosts_BlogPostId",
                table: "ClickLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ClickLogs_MarketingCampaigns_MarketingCampaignId",
                table: "ClickLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ClickLogs_Products_ProductId",
                table: "ClickLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ClickLogs_SocialTargets_SocialTargetId",
                table: "ClickLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_MarketingPostingQueue_MarketingCaptionVariations_MarketingCaptionVariationId",
                table: "MarketingPostingQueue");

            migrationBuilder.DropIndex(
                name: "IX_ClickLogs_IsFacebookTraffic",
                table: "ClickLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClickLogs_IsSocialTraffic",
                table: "ClickLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClickLogs_MarketingCampaignId_ClickType_ClickedAt",
                table: "ClickLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClickLogs_SocialTargetId_ClickType_ClickedAt",
                table: "ClickLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClickLogs_UtmCampaign",
                table: "ClickLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClickLogs_UtmContent",
                table: "ClickLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClickLogs_UtmMedium",
                table: "ClickLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClickLogs_UtmSource",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "IsFacebookTraffic",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "IsInstagramTraffic",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "IsPinterestTraffic",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "IsSocialTraffic",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "IsTelegramTraffic",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "LandingUrl",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "MarketingCampaignId",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "ReferrerUrl",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "SocialTargetId",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "UtmCampaign",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "UtmContent",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "UtmMedium",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "UtmSource",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "UtmTerm",
                table: "ClickLogs");

            migrationBuilder.AlterColumn<string>(
                name: "ClickType",
                table: "ClickLogs",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AddForeignKey(
                name: "FK_ClickLogs_BlogPosts_BlogPostId",
                table: "ClickLogs",
                column: "BlogPostId",
                principalTable: "BlogPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClickLogs_Products_ProductId",
                table: "ClickLogs",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MarketingPostingQueue_MarketingCaptionVariations_MarketingCaptionVariationId",
                table: "MarketingPostingQueue",
                column: "MarketingCaptionVariationId",
                principalTable: "MarketingCaptionVariations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
