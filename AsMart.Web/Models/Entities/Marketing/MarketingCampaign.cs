using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.Entities.Marketing
{
    public class MarketingCampaign
    {
        public int Id { get; set; }

        [Required, MaxLength(180)]
        public string Title { get; set; } = null!;

        [Required, MaxLength(220)]
        public string Slug { get; set; } = null!;

        public MarketingCampaignSourceType SourceType { get; set; }

        public int? ProductId { get; set; }
        public Product? Product { get; set; }

        public int? BlogPostId { get; set; }
        public BlogPost? BlogPost { get; set; }

        [MaxLength(800)]
        public string? CampaignUrl { get; set; }

        [MaxLength(800)]
        public string? ImageUrl { get; set; }

        [MaxLength(500)]
        public string? ShortDescription { get; set; }

        public MarketingCampaignStatus Status { get; set; } = MarketingCampaignStatus.Draft;

        public DateTime? ScheduledStartAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int MinDelayMinutes { get; set; } = 20;

        public int MaxDelayMinutes { get; set; } = 60;

        [MaxLength(120)]
        public string? UTMSource { get; set; } = "facebook";

        [MaxLength(120)]
        public string? UTMMedium { get; set; } = "social";

        [MaxLength(160)]
        public string? UTMCampaign { get; set; }

        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<MarketingCaptionVariation> CaptionVariations { get; set; } = new List<MarketingCaptionVariation>();

        public ICollection<MarketingPostingQueue> PostingQueueItems { get; set; } = new List<MarketingPostingQueue>();
    }
}