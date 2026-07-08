// Data/ApplicationDbContext.cs
using AsMart.Web.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AsMart.Web.Models.Entities.Marketing;

namespace AsMart.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<ProductTag> ProductTags => Set<ProductTag>();
        public DbSet<Collection> Collections => Set<Collection>();
        public DbSet<CollectionProduct> CollectionProducts => Set<CollectionProduct>();
        public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
        public DbSet<ClickLog> ClickLogs => Set<ClickLog>();
        public DbSet<Setting> Settings => Set<Setting>();
        public DbSet<UserProductStatus> UserProductStatuses => Set<UserProductStatus>();

        // NEW BLOG ENTITIES
        public DbSet<BlogPostCategory> BlogPostCategories => Set<BlogPostCategory>();
        public DbSet<BlogPostTag> BlogPostTags => Set<BlogPostTag>();
        public DbSet<BlogPostRating> BlogPostRatings => Set<BlogPostRating>();

        public DbSet<SeoPage> SeoPages => Set<SeoPage>();
        public DbSet<SeoPageProductSnapshot> SeoPageProductSnapshots => Set<SeoPageProductSnapshot>();

        public DbSet<MarketingChannel> MarketingChannels => Set<MarketingChannel>();
        public DbSet<SocialAccount> SocialAccounts => Set<SocialAccount>();
        public DbSet<SocialTarget> SocialTargets => Set<SocialTarget>();
        public DbSet<MarketingCampaign> MarketingCampaigns => Set<MarketingCampaign>();
        public DbSet<MarketingCaptionVariation> MarketingCaptionVariations => Set<MarketingCaptionVariation>();
        public DbSet<MarketingPostingQueue> MarketingPostingQueue => Set<MarketingPostingQueue>();
        public DbSet<MarketingPostingLog> MarketingPostingLogs => Set<MarketingPostingLog>();
        public DbSet<ApiClient> ApiClients { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Product
            builder.Entity<Product>(b =>
            {
                b.Property(p => p.Title)
                    .IsRequired()
                    .HasMaxLength(256);

                b.Property(p => p.ASIN)
                    .IsRequired()
                    .HasMaxLength(32);

                b.Property(p => p.Slug)
                    .IsRequired()
                    .HasMaxLength(256);

                b.HasIndex(p => p.ASIN).IsUnique();
                b.HasIndex(p => p.Slug).IsUnique();

                b.Property(p => p.Price).HasPrecision(18, 2);
                b.Property(p => p.ListPrice).HasPrecision(18, 2);
                b.Property(p => p.Rating).HasPrecision(5, 2);
            });


            // Category
            builder.Entity<Category>(b =>
            {
                b.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(128);

                b.Property(c => c.Slug)
                    .IsRequired()
                    .HasMaxLength(128);

                b.HasIndex(c => c.Slug).IsUnique();

                b.HasOne(c => c.ParentCategory)
                    .WithMany(c => c.Children)
                    .HasForeignKey(c => c.ParentCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.Property(c => c.Links)
                    .HasColumnName("Links")
                    .HasColumnType("nvarchar(max)")
                    .IsRequired(false);
            });

            // ProductCategory (many-to-many)
            builder.Entity<ProductCategory>(b =>
            {
                b.HasKey(pc => new { pc.ProductId, pc.CategoryId });

                b.HasOne(pc => pc.Product)
                    .WithMany(p => p.ProductCategories)
                    .HasForeignKey(pc => pc.ProductId);

                b.HasOne(pc => pc.Category)
                    .WithMany(c => c.ProductCategories)
                    .HasForeignKey(pc => pc.CategoryId);
            });

            // Tag
            builder.Entity<Tag>(b =>
            {
                b.Property(t => t.Name)
                    .IsRequired()
                    .HasMaxLength(64);

                b.Property(t => t.Slug)
                    .IsRequired()
                    .HasMaxLength(64);

                b.HasIndex(t => t.Slug).IsUnique();
            });

            // ProductTag (many-to-many)
            builder.Entity<ProductTag>(b =>
            {
                b.HasKey(pt => new { pt.ProductId, pt.TagId });

                b.HasOne(pt => pt.Product)
                    .WithMany(p => p.ProductTags)
                    .HasForeignKey(pt => pt.ProductId);

                b.HasOne(pt => pt.Tag)
                    .WithMany(t => t.ProductTags)
                    .HasForeignKey(pt => pt.TagId);
            });

            // Collection
            builder.Entity<Collection>(b =>
            {
                b.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(128);

                b.Property(c => c.Slug)
                    .IsRequired()
                    .HasMaxLength(128);

                b.HasIndex(c => c.Slug).IsUnique();
            });

            // CollectionProduct (many-to-many)
            builder.Entity<CollectionProduct>(b =>
            {
                b.HasKey(cp => new { cp.CollectionId, cp.ProductId });

                b.HasOne(cp => cp.Collection)
                    .WithMany(c => c.CollectionProducts)
                    .HasForeignKey(cp => cp.CollectionId);

                b.HasOne(cp => cp.Product)
                    .WithMany(p => p.CollectionProducts)
                    .HasForeignKey(cp => cp.ProductId);
            });

            // BlogPost
            builder.Entity<BlogPost>(b =>
            {
                b.Property(bp => bp.Title)
                    .IsRequired()
                    .HasMaxLength(256);

                b.Property(bp => bp.Slug)
                    .IsRequired()
                    .HasMaxLength(256);

                b.Property(bp => bp.ProductPageUrl)
                    .HasMaxLength(512);

                b.HasIndex(bp => bp.Slug).IsUnique();

                b.Property(bp => bp.MetaTitle)
                .HasMaxLength(256);

                b.Property(bp => bp.MetaDescription)
                 .HasMaxLength(512);

                b.Property(bp => bp.OgImageUrl)
                 .HasMaxLength(512);

            });

            // BlogPostCategory (many-to-many)
            builder.Entity<BlogPostCategory>(b =>
            {
                b.HasKey(x => new { x.BlogPostId, x.CategoryId });

                b.HasOne(x => x.BlogPost)
                    .WithMany(p => p.BlogPostCategories)
                    .HasForeignKey(x => x.BlogPostId);

                b.HasOne(x => x.Category)
                    .WithMany()
                    .HasForeignKey(x => x.CategoryId);
            });

            // BlogPostTag (many-to-many)
            builder.Entity<BlogPostTag>(b =>
            {
                b.HasKey(x => new { x.BlogPostId, x.TagId });

                b.HasOne(x => x.BlogPost)
                    .WithMany(p => p.BlogPostTags)
                    .HasForeignKey(x => x.BlogPostId);

                b.HasOne(x => x.Tag)
                    .WithMany()
                    .HasForeignKey(x => x.TagId);
            });

            // BlogPostRating
            builder.Entity<BlogPostRating>(b =>
            {
                b.Property(x => x.Value)
                    .IsRequired();

                b.HasIndex(x => new { x.BlogPostId, x.UserId }).IsUnique();

                b.HasOne(x => x.BlogPost)
                    .WithMany(p => p.Ratings)
                    .HasForeignKey(x => x.BlogPostId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Setting
            builder.Entity<Setting>(b =>
            {
                b.Property(s => s.Key)
                    .IsRequired()
                    .HasMaxLength(128);

                b.HasIndex(s => s.Key).IsUnique();
            });

            // UserProductStatus
            builder.Entity<UserProductStatus>(b =>
            {
                b.Property(x => x.State).IsRequired();
                b.Property(x => x.CreatedAt).IsRequired();

                b.HasOne(x => x.User)
                    .WithMany(u => u.ProductStatuses)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(x => new { x.UserId, x.ProductId, x.State });
            });

            // ClickLog (MERGED CONFIG)
            builder.Entity<ClickLog>(b =>
            {
                b.Property(x => x.ClickType)
                    .IsRequired()
                    .HasMaxLength(64);

                b.Property(x => x.UtmSource)
                    .HasMaxLength(120);

                b.Property(x => x.UtmMedium)
                    .HasMaxLength(120);

                b.Property(x => x.UtmCampaign)
                    .HasMaxLength(160);

                b.Property(x => x.UtmContent)
                    .HasMaxLength(160);

                b.Property(x => x.UtmTerm)
                    .HasMaxLength(160);

                b.Property(x => x.ReferrerUrl)
                    .HasMaxLength(1000);

                b.Property(x => x.LandingUrl)
                    .HasMaxLength(1000);

                b.HasOne<ApplicationUser>()
                    .WithMany(u => u.ClickLogs)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne(x => x.Product)
                    .WithMany(p => p.ClickLogs)
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne(x => x.BlogPost)
                    .WithMany(p => p.ClickLogs)
                    .HasForeignKey(x => x.BlogPostId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne(x => x.MarketingCampaign)
                    .WithMany()
                    .HasForeignKey(x => x.MarketingCampaignId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne(x => x.SocialTarget)
                    .WithMany()
                    .HasForeignKey(x => x.SocialTargetId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasIndex(x => x.ClickedAt);
                b.HasIndex(x => x.UtmSource);
                b.HasIndex(x => x.UtmMedium);
                b.HasIndex(x => x.UtmCampaign);
                b.HasIndex(x => x.UtmContent);
                b.HasIndex(x => x.IsSocialTraffic);
                b.HasIndex(x => x.IsFacebookTraffic);

                b.HasIndex(x => new { x.ProductId, x.ClickType, x.ClickedAt });
                b.HasIndex(x => new { x.BlogPostId, x.ClickType, x.ClickedAt });
                b.HasIndex(x => new { x.MarketingCampaignId, x.ClickType, x.ClickedAt });
                b.HasIndex(x => new { x.SocialTargetId, x.ClickType, x.ClickedAt });
            });

            // SEO
            builder.Entity<SeoPage>(b =>
            {
                b.ToTable("SeoPages");

                b.HasKey(x => x.Id);

                b.Property(x => x.Slug).IsRequired().HasMaxLength(200);
                b.Property(x => x.Title).IsRequired().HasMaxLength(200);
                b.Property(x => x.MetaDescription).HasMaxLength(320);
                b.Property(x => x.H1).HasMaxLength(220);

                b.Property(x => x.TemplateKey).IsRequired().HasMaxLength(50);
                b.Property(x => x.TargetKeyword).IsRequired().HasMaxLength(260);

                b.Property(x => x.Brand).HasMaxLength(120);
                b.Property(x => x.SortMode).IsRequired().HasMaxLength(30);

                b.Property(x => x.PriceMin).HasPrecision(18, 2);
                b.Property(x => x.PriceMax).HasPrecision(18, 2);

                b.Property(x => x.Status).IsRequired();
                b.Property(x => x.PublishedAt);
                b.Property(x => x.UpdatedAt).IsRequired();
            });

            builder.Entity<SeoPageProductSnapshot>(b =>
            {
                b.ToTable("SeoPageProductSnapshots");

                b.HasKey(x => x.Id);

                b.Property(x => x.SeoPageId).IsRequired();
                b.Property(x => x.ProductId).IsRequired();
                b.Property(x => x.RankNo).IsRequired();
                b.Property(x => x.CreatedAt).IsRequired();

                b.HasIndex(x => new { x.SeoPageId, x.RankNo });
                b.HasIndex(x => new { x.SeoPageId, x.ProductId });
            });

            builder.Entity<MarketingChannel>(b =>
            {
                b.ToTable("MarketingChannels");

                b.Property(x => x.Name).IsRequired().HasMaxLength(80);
                b.Property(x => x.Notes).HasMaxLength(500);

                b.HasIndex(x => new { x.Platform, x.Name }).IsUnique();
            });

            builder.Entity<SocialAccount>(b =>
            {
                b.ToTable("SocialAccounts");

                b.Property(x => x.DisplayName).IsRequired().HasMaxLength(120);
                b.Property(x => x.ExternalAccountId).HasMaxLength(256);
                b.Property(x => x.ProfileUrl).HasMaxLength(512);
                b.Property(x => x.Notes).HasMaxLength(1000);

                b.HasOne(x => x.MarketingChannel)
                    .WithMany(x => x.SocialAccounts)
                    .HasForeignKey(x => x.MarketingChannelId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(x => new { x.MarketingChannelId, x.DisplayName });
            });

            builder.Entity<SocialTarget>(b =>
            {
                b.ToTable("SocialTargets");

                b.Property(x => x.Name).IsRequired().HasMaxLength(200);
                b.Property(x => x.TargetUrl).HasMaxLength(512);
                b.Property(x => x.ExternalTargetId).HasMaxLength(256);
                b.Property(x => x.Niche).HasMaxLength(120);
                b.Property(x => x.Notes).HasMaxLength(1000);

                b.HasOne(x => x.SocialAccount)
                    .WithMany(x => x.SocialTargets)
                    .HasForeignKey(x => x.SocialAccountId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(x => new { x.SocialAccountId, x.TargetType, x.Name });
            });

            builder.Entity<MarketingCampaign>(b =>
            {
                b.ToTable("MarketingCampaigns");

                b.Property(x => x.Title).IsRequired().HasMaxLength(180);
                b.Property(x => x.Slug).IsRequired().HasMaxLength(220);
                b.Property(x => x.CampaignUrl).HasMaxLength(800);
                b.Property(x => x.ImageUrl).HasMaxLength(800);
                b.Property(x => x.ShortDescription).HasMaxLength(500);
                b.Property(x => x.UTMSource).HasMaxLength(120);
                b.Property(x => x.UTMMedium).HasMaxLength(120);
                b.Property(x => x.UTMCampaign).HasMaxLength(160);

                b.HasIndex(x => x.Slug).IsUnique();
                b.HasIndex(x => x.Status);
                b.HasIndex(x => x.CreatedAt);

                b.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne(x => x.BlogPost)
                    .WithMany()
                    .HasForeignKey(x => x.BlogPostId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne(x => x.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<MarketingCaptionVariation>(b =>
            {
                b.ToTable("MarketingCaptionVariations");

                b.Property(x => x.CaptionText).IsRequired();

                b.HasOne(x => x.MarketingCampaign)
                    .WithMany(x => x.CaptionVariations)
                    .HasForeignKey(x => x.MarketingCampaignId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(x => new { x.MarketingCampaignId, x.SortOrder });
            });

            builder.Entity<MarketingPostingQueue>(b =>
            {
                b.ToTable("MarketingPostingQueue");

                b.Property(x => x.FinalPostText).HasMaxLength(1200);
                b.Property(x => x.FinalUrlWithUtm).HasMaxLength(1000);
                b.Property(x => x.PublishedPostUrl).HasMaxLength(1000);
                b.Property(x => x.LastError).HasMaxLength(2000);

                b.HasOne(x => x.MarketingCampaign)
                    .WithMany(x => x.PostingQueueItems)
                    .HasForeignKey(x => x.MarketingCampaignId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.SocialTarget)
                    .WithMany()
                    .HasForeignKey(x => x.SocialTargetId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.MarketingCaptionVariation)
                    .WithMany()
                    .HasForeignKey(x => x.MarketingCaptionVariationId)
                    .OnDelete(DeleteBehavior.NoAction);

                b.HasIndex(x => x.Status);
                b.HasIndex(x => x.ScheduledAt);
                b.HasIndex(x => new { x.MarketingCampaignId, x.SocialTargetId });
            });

            builder.Entity<MarketingPostingLog>(b =>
            {
                b.ToTable("MarketingPostingLogs");

                b.Property(x => x.Message).HasMaxLength(2000);
                b.Property(x => x.ScreenshotPath).HasMaxLength(1000);

                b.HasOne(x => x.MarketingPostingQueue)
                    .WithMany(x => x.Logs)
                    .HasForeignKey(x => x.MarketingPostingQueueId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(x => x.CreatedAt);
            });


            builder.Entity<ApiClient>(b =>
            {
                b.ToTable("ApiClients");

                b.HasKey(x => x.Id);

                b.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                b.Property(x => x.ApiKey)
                    .IsRequired()
                    .HasMaxLength(200);

                b.Property(x => x.Website)
                    .HasMaxLength(300);

                b.Property(x => x.RateLimitPerMinute)
                    .IsRequired();

                b.Property(x => x.CreatedAt)
                    .IsRequired();

                b.HasIndex(x => x.ApiKey)
                    .IsUnique();

                b.HasOne(x => x.User)
                    .WithMany(x => x.ApiClients)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

        }
    }
}
