using AsMart.Web.Models.Entities.Marketing;

namespace AsMart.Web.Models.ViewModels.Marketing
{
    public class MarketingQueueIndexViewModel
    {
        public string StatusFilter { get; set; } = "all";
        public string? Search { get; set; }

        public int TotalItems { get; set; }
        public int PendingItems { get; set; }
        public int ScheduledItems { get; set; }
        public int PostedItems { get; set; }
        public int FailedItems { get; set; }

        public List<MarketingQueueItemViewModel> Items { get; set; } = new();

        public class MarketingQueueItemViewModel
        {
            public int Id { get; set; }

            public int CampaignId { get; set; }
            public string CampaignTitle { get; set; } = "";
            public string CampaignSlug { get; set; } = "";

            public string TargetName { get; set; } = "";
            public string? TargetUrl { get; set; }
            public MarketingTargetType TargetType { get; set; }

            public MarketingQueueStatus Status { get; set; }
            public MarketingPublishMode PublishMode { get; set; }

            public DateTime CreatedAt { get; set; }
            public DateTime? ScheduledAt { get; set; }
            public DateTime? PostedAt { get; set; }

            public string? FinalPostText { get; set; }
            public string? FinalUrlWithUtm { get; set; }
            public string? PublishedPostUrl { get; set; }
            public string? LastError { get; set; }

            public int RetryCount { get; set; }
        }
    }
}