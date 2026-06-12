using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsMart.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialMarketingAutomationPhase0Phase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketingCampaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    BlogPostId = table.Column<int>(type: "int", nullable: true),
                    CampaignUrl = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true),
                    ShortDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ScheduledStartAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MinDelayMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxDelayMinutes = table.Column<int>(type: "int", nullable: false),
                    UTMSource = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    UTMMedium = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    UTMCampaign = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingCampaigns_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MarketingCampaigns_BlogPosts_BlogPostId",
                        column: x => x.BlogPostId,
                        principalTable: "BlogPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MarketingCampaigns_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MarketingChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Platform = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingChannels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketingCaptionVariations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketingCampaignId = table.Column<int>(type: "int", nullable: false),
                    CaptionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingCaptionVariations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingCaptionVariations_MarketingCampaigns_MarketingCampaignId",
                        column: x => x.MarketingCampaignId,
                        principalTable: "MarketingCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocialAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketingChannelId = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ExternalAccountId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ProfileUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PublishMode = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccessTokenEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshTokenEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialAccounts_MarketingChannels_MarketingChannelId",
                        column: x => x.MarketingChannelId,
                        principalTable: "MarketingChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocialTargets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SocialAccountId = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TargetUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ExternalTargetId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Niche = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastPostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DailyPostLimit = table.Column<int>(type: "int", nullable: false),
                    MinDelayMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxDelayMinutes = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialTargets_SocialAccounts_SocialAccountId",
                        column: x => x.SocialAccountId,
                        principalTable: "SocialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketingPostingQueue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketingCampaignId = table.Column<int>(type: "int", nullable: false),
                    SocialTargetId = table.Column<int>(type: "int", nullable: false),
                    MarketingCaptionVariationId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PublishMode = table.Column<int>(type: "int", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    FinalPostText = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: true),
                    FinalUrlWithUtm = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PublishedPostUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingPostingQueue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingPostingQueue_MarketingCampaigns_MarketingCampaignId",
                        column: x => x.MarketingCampaignId,
                        principalTable: "MarketingCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketingPostingQueue_MarketingCaptionVariations_MarketingCaptionVariationId",
                        column: x => x.MarketingCaptionVariationId,
                        principalTable: "MarketingCaptionVariations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_MarketingPostingQueue_SocialTargets_SocialTargetId",
                        column: x => x.SocialTargetId,
                        principalTable: "SocialTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketingPostingLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketingPostingQueueId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ScreenshotPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingPostingLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingPostingLogs_MarketingPostingQueue_MarketingPostingQueueId",
                        column: x => x.MarketingPostingQueueId,
                        principalTable: "MarketingPostingQueue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingCampaigns_BlogPostId",
                table: "MarketingCampaigns",
                column: "BlogPostId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingCampaigns_CreatedAt",
                table: "MarketingCampaigns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingCampaigns_CreatedByUserId",
                table: "MarketingCampaigns",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingCampaigns_ProductId",
                table: "MarketingCampaigns",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingCampaigns_Slug",
                table: "MarketingCampaigns",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketingCampaigns_Status",
                table: "MarketingCampaigns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingCaptionVariations_MarketingCampaignId_SortOrder",
                table: "MarketingCaptionVariations",
                columns: new[] { "MarketingCampaignId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingChannels_Platform_Name",
                table: "MarketingChannels",
                columns: new[] { "Platform", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketingPostingLogs_CreatedAt",
                table: "MarketingPostingLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingPostingLogs_MarketingPostingQueueId",
                table: "MarketingPostingLogs",
                column: "MarketingPostingQueueId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingPostingQueue_MarketingCampaignId_SocialTargetId",
                table: "MarketingPostingQueue",
                columns: new[] { "MarketingCampaignId", "SocialTargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingPostingQueue_MarketingCaptionVariationId",
                table: "MarketingPostingQueue",
                column: "MarketingCaptionVariationId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingPostingQueue_ScheduledAt",
                table: "MarketingPostingQueue",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingPostingQueue_SocialTargetId",
                table: "MarketingPostingQueue",
                column: "SocialTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingPostingQueue_Status",
                table: "MarketingPostingQueue",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SocialAccounts_MarketingChannelId_DisplayName",
                table: "SocialAccounts",
                columns: new[] { "MarketingChannelId", "DisplayName" });

            migrationBuilder.CreateIndex(
                name: "IX_SocialTargets_SocialAccountId_TargetType_Name",
                table: "SocialTargets",
                columns: new[] { "SocialAccountId", "TargetType", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketingPostingLogs");

            migrationBuilder.DropTable(
                name: "MarketingPostingQueue");

            migrationBuilder.DropTable(
                name: "MarketingCaptionVariations");

            migrationBuilder.DropTable(
                name: "SocialTargets");

            migrationBuilder.DropTable(
                name: "MarketingCampaigns");

            migrationBuilder.DropTable(
                name: "SocialAccounts");

            migrationBuilder.DropTable(
                name: "MarketingChannels");
        }
    }
}
