using AsMart.Web.Models.Entities.Marketing;

namespace AsMart.Web.Models.ViewModels.Marketing
{
    public class MarketingDashboardViewModel
    {
        public int TotalChannels { get; set; }
        public int TotalSocialAccounts { get; set; }
        public int TotalFacebookTargets { get; set; }
        public int ActiveFacebookTargets { get; set; }
        public int TotalCampaigns { get; set; }
        public int DraftCampaigns { get; set; }
        public int ReadyCampaigns { get; set; }
        public int ScheduledCampaigns { get; set; }
        public int RunningCampaigns { get; set; }
        public int CompletedCampaigns { get; set; }
        public int PendingQueueItems { get; set; }
        public int ScheduledQueueItems { get; set; }
        public int PostedQueueItems { get; set; }
        public int FailedQueueItems { get; set; }
        public int PostsToday { get; set; }
        public int PostsLast7Days { get; set; }
        public int PostsLast30Days { get; set; }

        public List<RecentCampaignItem> RecentCampaigns { get; set; } = new();
        public List<RecentQueueItem> RecentQueueItems { get; set; } = new();
        public List<TopTargetItem> TopTargets { get; set; } = new();

        public class RecentCampaignItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public MarketingCampaignSourceType SourceType { get; set; }
            public MarketingCampaignStatus Status { get; set; }
            public DateTime CreatedAt { get; set; }
            public int TotalQueueItems { get; set; }
            public int PostedItems { get; set; }
            public int FailedItems { get; set; }
        }

        public class RecentQueueItem
        {
            public int Id { get; set; }
            public string CampaignTitle { get; set; } = "";
            public string TargetName { get; set; } = "";
            public MarketingTargetType TargetType { get; set; }
            public MarketingQueueStatus Status { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ScheduledAt { get; set; }
            public DateTime? PostedAt { get; set; }
        }

        public class TopTargetItem
        {
            public int TargetId { get; set; }
            public string TargetName { get; set; } = "";
            public MarketingTargetType TargetType { get; set; }
            public int TotalPosts { get; set; }
            public int PostedPosts { get; set; }
            public int FailedPosts { get; set; }
        }
    }
}