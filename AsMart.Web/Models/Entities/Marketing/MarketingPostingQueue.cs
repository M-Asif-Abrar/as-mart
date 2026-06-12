using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.Entities.Marketing
{
    public class MarketingPostingQueue
    {
        public int Id { get; set; }

        public int MarketingCampaignId { get; set; }
        public MarketingCampaign? MarketingCampaign { get; set; }

        public int SocialTargetId { get; set; }
        public SocialTarget? SocialTarget { get; set; }

        public int? MarketingCaptionVariationId { get; set; }
        public MarketingCaptionVariation? MarketingCaptionVariation { get; set; }

        public MarketingQueueStatus Status { get; set; } = MarketingQueueStatus.Pending;

        public MarketingPublishMode PublishMode { get; set; } = MarketingPublishMode.Manual;

        public DateTime? ScheduledAt { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? PostedAt { get; set; }

        public int RetryCount { get; set; }

        [MaxLength(1200)]
        public string? FinalPostText { get; set; }

        [MaxLength(1000)]
        public string? FinalUrlWithUtm { get; set; }

        [MaxLength(1000)]
        public string? PublishedPostUrl { get; set; }

        [MaxLength(2000)]
        public string? LastError { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<MarketingPostingLog> Logs { get; set; } = new List<MarketingPostingLog>();
    }
}