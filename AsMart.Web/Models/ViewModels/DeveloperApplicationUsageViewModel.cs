namespace AsMart.Web.Models.ViewModels
{
    public sealed class DeveloperApplicationUsageViewModel
    {
        public int ApplicationId { get; init; }

        public string ApplicationName { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public string MaskedApiKey { get; init; } = string.Empty;

        public int SelectedDays { get; init; } = 30;

        public DateTime PeriodStartUtc { get; init; }

        public DateTime PeriodEndUtc { get; init; }

        public long TotalRequests { get; init; }

        public long SuccessfulRequests { get; init; }

        public long FailedRequests { get; init; }

        public double SuccessRate { get; init; }

        public double AverageResponseTimeMs { get; init; }

        public long RequestsToday { get; init; }

        public long RequestsThisMonth { get; init; }

        public int MonthlyQuota { get; init; }

        public long RemainingQuota { get; init; }

        public bool HasUnlimitedQuota =>
            MonthlyQuota <= 0;

        // Request-log filters
        public string EndpointFilter { get; init; } = string.Empty;

        public string MethodFilter { get; init; } = "all";

        public string StatusFilter { get; init; } = "all";

        public DateTime? FromDateUtc { get; init; }

        public DateTime? ToDateUtc { get; init; }

        public int PageNumber { get; init; } = 1;

        public int PageSize { get; init; } = 25;

        public int TotalFilteredRequests { get; init; }

        public int TotalPages =>
            TotalFilteredRequests <= 0
                ? 1
                : (int)Math.Ceiling(
                    TotalFilteredRequests /
                    (double)Math.Max(PageSize, 1));

        public bool HasPreviousPage =>
            PageNumber > 1;

        public bool HasNextPage =>
            PageNumber < TotalPages;

        public IReadOnlyList<DeveloperUsageDailyVm>
            DailyUsage
        { get; init; }
                = Array.Empty<DeveloperUsageDailyVm>();

        public IReadOnlyList<DeveloperUsageEndpointVm>
            EndpointUsage
        { get; init; }
                = Array.Empty<DeveloperUsageEndpointVm>();

        public IReadOnlyList<DeveloperUsageStatusVm>
            StatusUsage
        { get; init; }
                = Array.Empty<DeveloperUsageStatusVm>();

        public IReadOnlyList<DeveloperUsageRequestVm>
            RecentRequests
        { get; init; }
                = Array.Empty<DeveloperUsageRequestVm>();
    }

    public sealed class DeveloperUsageDailyVm
    {
        public DateTime DateUtc { get; init; }

        public long Requests { get; init; }

        public long SuccessfulRequests { get; init; }

        public long FailedRequests { get; init; }

        public double AverageResponseTimeMs { get; init; }
    }

    public sealed class DeveloperUsageEndpointVm
    {
        public string Endpoint { get; init; } = string.Empty;

        public long Requests { get; init; }

        public long SuccessfulRequests { get; init; }

        public long FailedRequests { get; init; }

        public double AverageResponseTimeMs { get; init; }
    }

    public sealed class DeveloperUsageStatusVm
    {
        public int StatusCode { get; init; }

        public long Requests { get; init; }
    }

    public sealed class DeveloperUsageRequestVm
    {
        public long Id { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public string HttpMethod { get; init; } = string.Empty;

        public string Endpoint { get; init; } = string.Empty;

        public string? QueryString { get; init; }

        public string ApiVersion { get; init; } = string.Empty;

        public int StatusCode { get; init; }

        public long ResponseTimeMs { get; init; }

        public string? IpAddress { get; init; }

        public string? UserAgent { get; init; }
    }
}