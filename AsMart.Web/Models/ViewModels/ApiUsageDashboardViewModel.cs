namespace AsMart.Web.Models.ViewModels
{
    public class ApiUsageDashboardViewModel
    {
        // Dashboard summary
        public int TotalRequests { get; set; }
        public int RequestsToday { get; set; }
        public int FailedRequestsToday { get; set; }
        public int SuccessfulRequestsToday { get; set; }
        public int PublicRequests { get; set; }
        public int ApiKeyRequests { get; set; }
        public double AverageResponseTimeMs { get; set; }

        // Dashboard breakdowns
        public List<ApiEndpointUsageVm> TopEndpoints { get; set; } = new();
        public List<ApiClientUsageVm> TopClients { get; set; } = new();
        public List<ApiStatusUsageVm> StatusSummary { get; set; } = new();

        // Filter options
        public List<ApiClientFilterOptionVm> ClientOptions { get; set; } = new();

        // Selected filters
        public int? ClientId { get; set; }
        public string? Endpoint { get; set; }
        public int? StatusCode { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string RequestType { get; set; } = "all";
        public long? MinimumResponseTimeMs { get; set; }

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalFilteredRequests { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;

        public int FirstItemNumber =>
            TotalFilteredRequests == 0
                ? 0
                : ((Page - 1) * PageSize) + 1;

        public int LastItemNumber =>
            Math.Min(Page * PageSize, TotalFilteredRequests);

        public bool HasActiveFilters =>
            ClientId.HasValue ||
            !string.IsNullOrWhiteSpace(Endpoint) ||
            StatusCode.HasValue ||
            From.HasValue ||
            To.HasValue ||
            !string.Equals(RequestType, "all", StringComparison.OrdinalIgnoreCase) ||
            MinimumResponseTimeMs.HasValue;

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

    public class ApiClientFilterOptionVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
    }

    public class ApiRecentUsageVm
    {
        public long Id { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string? QueryString { get; set; }
        public string Method { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public long ResponseTimeMs { get; set; }
        public int? ApiClientId { get; set; }
        public string ClientName { get; set; } = "Public";
        public DateTime CreatedAt { get; set; }
    }
}
