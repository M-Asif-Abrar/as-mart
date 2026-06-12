namespace AsMart.Web.Models.ViewModels.Marketing
{
    public class MarketingClickAnalyticsViewModel
    {
        public int TotalSocialClicks { get; set; }
        public int FacebookClicks { get; set; }
        public int InstagramClicks { get; set; }
        public int PinterestClicks { get; set; }
        public int TelegramClicks { get; set; }

        public int BlogLandingClicks { get; set; }
        public int ProductLandingClicks { get; set; }
        public int AmazonOutboundClicks { get; set; }

        public List<CampaignClickItem> CampaignClicks { get; set; } = new();
        public List<TargetClickItem> TargetClicks { get; set; } = new();
        public List<BlogClickItem> BlogClicks { get; set; } = new();
        public List<ProductClickItem> ProductClicks { get; set; } = new();
        public List<UtmCampaignItem> UtmCampaigns { get; set; } = new();

        public class CampaignClickItem
        {
            public int? CampaignId { get; set; }
            public string CampaignName { get; set; } = "";
            public string UtmCampaign { get; set; } = "";
            public int Clicks { get; set; }
            public int FacebookClicks { get; set; }
            public int BlogClicks { get; set; }
            public int ProductClicks { get; set; }
        }

        public class TargetClickItem
        {
            public int? TargetId { get; set; }
            public string TargetName { get; set; } = "";
            public string UtmContent { get; set; } = "";
            public int Clicks { get; set; }
            public int FacebookClicks { get; set; }
        }

        public class BlogClickItem
        {
            public int BlogPostId { get; set; }
            public string Title { get; set; } = "";
            public string Slug { get; set; } = "";
            public int Clicks { get; set; }
        }

        public class ProductClickItem
        {
            public int ProductId { get; set; }
            public string Title { get; set; } = "";
            public string Slug { get; set; } = "";
            public int Clicks { get; set; }
        }

        public class UtmCampaignItem
        {
            public string UtmCampaign { get; set; } = "";
            public string UtmSource { get; set; } = "";
            public string UtmMedium { get; set; } = "";
            public int Clicks { get; set; }
            public DateTime LastClickAt { get; set; }
        }
    }
}