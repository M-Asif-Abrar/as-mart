namespace AsMart.Web.Models.ViewModels
{
    public class MyApiUsageViewModel
    {
        public int ApiClientId { get; set; }

        public string ClientName { get; set; } = string.Empty;

        public string MaskedApiKey { get; set; } = string.Empty;

        public string? Website { get; set; }

        public bool IsActive { get; set; }

        public int RateLimitPerMinute { get; set; }

        public int MonthlyQuota { get; set; }

        public int RequestsThisMonth { get; set; }

        public int RemainingQuota { get; set; }

        public int RequestsToday { get; set; }

        public int FailedRequestsThisMonth { get; set; }

        public double SuccessRate { get; set; }

        public double AverageResponseTimeMs { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastUsedAt { get; set; }

        public DateTime QuotaResetAt { get; set; }

        public List<MyApiEndpointUsageVm> TopEndpoints { get; set; }
            = new();

        public List<MyApiRecentRequestVm> RecentRequests { get; set; }
            = new();
    }

    public class MyApiEndpointUsageVm
    {
        public string Endpoint { get; set; } = string.Empty;

        public int RequestCount { get; set; }
    }

    public class MyApiRecentRequestVm
    {
        public DateTime CreatedAt { get; set; }

        public string HttpMethod { get; set; } = string.Empty;

        public string Endpoint { get; set; } = string.Empty;

        public string? QueryString { get; set; }

        public int StatusCode { get; set; }

        public long ResponseTimeMs { get; set; }
    }
}