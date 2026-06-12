using AsMart.Web.Models.Entities.Marketing;

namespace AsMart.Web.Models.ViewModels.Marketing
{
    public class MarketingAnalyticsViewModel
    {
        public int TotalCampaigns { get; set; }
        public int TotalQueueItems { get; set; }
        public int PostedItems { get; set; }
        public int FailedItems { get; set; }
        public int ScheduledItems { get; set; }
        public int PendingItems { get; set; }

        public int PostsToday { get; set; }
        public int PostsLast7Days { get; set; }
        public int PostsLast30Days { get; set; }

        public decimal SuccessRate { get; set; }
        public decimal FailureRate { get; set; }

        public List<CampaignPerformanceItem> CampaignPerformance { get; set; } = new();
        public List<TargetPerformanceItem> TargetPerformance { get; set; } = new();
        public List<DailyPostingItem> DailyPostingStats { get; set; } = new();

        public class CampaignPerformanceItem
        {
            public int CampaignId { get; set; }
            public string CampaignTitle { get; set; } = "";
            public MarketingCampaignStatus CampaignStatus { get; set; }
            public int TotalQueueItems { get; set; }
            public int PostedItems { get; set; }
            public int FailedItems { get; set; }
            public int ScheduledItems { get; set; }
            public decimal SuccessRate { get; set; }
        }

        public class TargetPerformanceItem
        {
            public int TargetId { get; set; }
            public string TargetName { get; set; } = "";
            public MarketingTargetType TargetType { get; set; }
            public int TotalQueueItems { get; set; }
            public int PostedItems { get; set; }
            public int FailedItems { get; set; }
            public DateTime? LastPostedAt { get; set; }
            public decimal SuccessRate { get; set; }
        }

        public class DailyPostingItem
        {
            public DateTime Date { get; set; }
            public int PostedItems { get; set; }
            public int FailedItems { get; set; }
            public int ScheduledItems { get; set; }
        }
    }
}