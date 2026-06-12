using AsMart.Web.Models.Entities.Marketing;

namespace AsMart.Web.Models.Entities
{
    public class ClickLog
    {
        public int Id { get; set; }

        // ------------------------------------------------
        // PRODUCT / BLOG
        // ------------------------------------------------

        public int? ProductId { get; set; }
        public Product? Product { get; set; }

        public int? BlogPostId { get; set; }
        public BlogPost? BlogPost { get; set; }

        // ------------------------------------------------
        // MARKETING ATTRIBUTION
        // ------------------------------------------------

        public int? MarketingCampaignId { get; set; }
        public MarketingCampaign? MarketingCampaign { get; set; }

        public int? SocialTargetId { get; set; }
        public SocialTarget? SocialTarget { get; set; }

        // ------------------------------------------------
        // CLICK TYPE
        // ------------------------------------------------

        // Examples:
        // AmazonOutbound
        // BlogBuy
        // BlogView
        // SocialCampaignVisit
        public string ClickType { get; set; } = "AmazonOutbound";

        // ------------------------------------------------
        // UTM TRACKING
        // ------------------------------------------------

        public string? UtmSource { get; set; }
        public string? UtmMedium { get; set; }
        public string? UtmCampaign { get; set; }
        public string? UtmContent { get; set; }
        public string? UtmTerm { get; set; }

        // ------------------------------------------------
        // REQUEST DATA
        // ------------------------------------------------

        public string? ReferrerUrl { get; set; }
        public string? LandingUrl { get; set; }

        public DateTime ClickedAt { get; set; }

        public string? UserId { get; set; }

        public string? IPAddress { get; set; }

        public string? UserAgent { get; set; }

        // ------------------------------------------------
        // SOCIAL FLAGS
        // ------------------------------------------------

        public bool IsSocialTraffic { get; set; }

        public bool IsFacebookTraffic { get; set; }

        public bool IsTelegramTraffic { get; set; }

        public bool IsPinterestTraffic { get; set; }

        public bool IsInstagramTraffic { get; set; }
    }
}