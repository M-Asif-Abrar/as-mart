namespace AsMart.Web.Models.ViewModels
{
    public class ApiUsageDashboardViewModel
    {
        public int TotalRequests { get; set; }
        public int RequestsToday { get; set; }
        public int FailedRequestsToday { get; set; }
        public int SuccessfulRequestsToday { get; set; }
        public int PublicRequests { get; set; }
        public int ApiKeyRequests { get; set; }
        public double AverageResponseTimeMs { get; set; }

        public List<ApiEndpointUsageVm> TopEndpoints { get; set; } = new();
        public List<ApiClientUsageVm> TopClients { get; set; } = new();
        public List<ApiStatusUsageVm> StatusSummary { get; set; } = new();
        public List<ApiRecentUsageVm> RecentLogs { get; set; } = new();
    }

    public class ApiEndpointUsageVm
    {
        public string Endpoint { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ApiClientUsageVm
    {
        public string ClientName { get; set; } = string.Empty;
        public string? WebsiteUrl { get; set; }
        public int Count { get; set; }
        public DateTime? LastUsedAt { get; set; }
    }

    public class ApiStatusUsageVm
    {
        public int StatusCode { get; set; }
        public int Count { get; set; }
    }

    public class ApiRecentUsageVm
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public long ResponseTimeMs { get; set; }
        public string? ClientName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}