namespace AsMart.Web.Models.ViewModels
{
    public sealed class AdminDashboardViewModel
    {
        // Core totals
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int FeaturedProducts { get; set; }
        public int DealProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalBlogPosts { get; set; }
        public int VisibleBlogPosts { get; set; }
        public int TotalCollections { get; set; }
        public int TotalClickLogs { get; set; }
        public int TotalUserProductStatuses { get; set; }

        // Engagement
        public int ClicksToday { get; set; }
        public int ClicksLast7Days { get; set; }
        public int ClicksLast30Days { get; set; }
        public int SocialClicksLast30Days { get; set; }
        public int FacebookClicksLast30Days { get; set; }
        public decimal AverageProductRating { get; set; }

        // API platform
        public int TotalApiClients { get; set; }
        public int ActiveApiClients { get; set; }
        public int ApiRequestsToday { get; set; }
        public int ApiRequestsLast7Days { get; set; }
        public int ApiRequestsLast30Days { get; set; }
        public int ApiErrorsLast30Days { get; set; }
        public double ApiSuccessRateLast30Days { get; set; }

        // JWT / security
        public int ActiveRefreshTokens { get; set; }
        public int RevokedRefreshTokens { get; set; }
        public int ExpiredRefreshTokens { get; set; }

        // Chart and table data
        public List<CategoryProductsItem> ProductsPerCategory { get; set; } = [];
        public List<CollectionProductsItem> ProductsPerCollection { get; set; } = [];
        public List<DailyMetricItem> DailyClicks { get; set; } = [];
        public List<DailyMetricItem> DailyApiRequests { get; set; } = [];
        public List<NamedCountItem> TrafficSources { get; set; } = [];
        public List<NamedCountItem> ApiStatusGroups { get; set; } = [];
        public List<TopProductClicksItem> TopProductsByClicks { get; set; } = [];
        public List<TopCategoryClicksItem> TopCategoriesByClicks { get; set; } = [];
        public List<TopApiEndpointItem> TopApiEndpoints { get; set; } = [];
        public List<RecentClickItem> RecentClicks { get; set; } = [];

        public sealed class CategoryProductsItem
        {
            public string CategoryName { get; set; } = string.Empty;
            public int ProductCount { get; set; }
        }

        public sealed class CollectionProductsItem
        {
            public string CollectionName { get; set; } = string.Empty;
            public int ProductCount { get; set; }
        }

        public sealed class DailyMetricItem
        {
            public DateTime Date { get; set; }
            public int Count { get; set; }
        }

        public sealed class NamedCountItem
        {
            public string Name { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        public sealed class TopProductClicksItem
        {
            public int ProductId { get; set; }
            public string Slug { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string? CategoryName { get; set; }
            public int Clicks { get; set; }
            public DateTime? LastClickedAt { get; set; }
        }

        public sealed class TopCategoryClicksItem
        {
            public string CategoryName { get; set; } = string.Empty;
            public int Clicks { get; set; }
        }

        public sealed class TopApiEndpointItem
        {
            public string Endpoint { get; set; } = string.Empty;
            public int Requests { get; set; }
            public int Errors { get; set; }
            public DateTime LastRequestedAt { get; set; }
        }

        public sealed class RecentClickItem
        {
            public DateTime ClickedAt { get; set; }
            public string ClickType { get; set; } = string.Empty;
            public string? ProductTitle { get; set; }
            public string? UtmSource { get; set; }
            public bool IsSocialTraffic { get; set; }
        }
    }
}
