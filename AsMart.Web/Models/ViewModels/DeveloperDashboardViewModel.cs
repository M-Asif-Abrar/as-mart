namespace AsMart.Web.Models.ViewModels
{
    public sealed class DeveloperDashboardViewModel
    {
        public string DisplayName { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public int TotalApiKeys { get; init; }

        public int ActiveApiKeys { get; init; }

        public int ExpiredApiKeys { get; init; }

        public int RevokedApiKeys { get; init; }

        public long RequestsToday { get; init; }

        public long RequestsThisMonth { get; init; }

        public long SuccessfulRequestsThisMonth { get; init; }

        public long FailedRequestsThisMonth { get; init; }

        public double SuccessRate { get; init; }

        public double AverageResponseTimeMs { get; init; }

        public long TotalMonthlyQuota { get; init; }

        public long RemainingMonthlyQuota { get; init; }

        public double QuotaUsagePercentage { get; init; }

        public IReadOnlyList<DeveloperApiKeySummaryVm> ApiKeys { get; init; }
            = Array.Empty<DeveloperApiKeySummaryVm>();

        public IReadOnlyList<DeveloperDailyUsageVm> DailyUsage { get; init; }
            = Array.Empty<DeveloperDailyUsageVm>();

        public IReadOnlyList<DeveloperEndpointUsageVm> TopEndpoints { get; init; }
            = Array.Empty<DeveloperEndpointUsageVm>();

        public IReadOnlyList<DeveloperRecentRequestVm> RecentRequests { get; init; }
            = Array.Empty<DeveloperRecentRequestVm>();
    }

    public sealed class DeveloperApiKeySummaryVm
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string MaskedApiKey { get; init; } = string.Empty;

        public string? Website { get; init; }

        public string Status { get; init; } = string.Empty;

        public bool IsUsable { get; init; }

        public int RateLimitPerMinute { get; init; }

        public int MonthlyQuota { get; init; }

        public long RequestsThisMonth { get; init; }

        public long RemainingQuota { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public DateTime? LastUsedAtUtc { get; init; }

        public DateTime? ExpiresAtUtc { get; init; }
    }

    public sealed class DeveloperDailyUsageVm
    {
        public DateTime DateUtc { get; init; }

        public int Requests { get; init; }

        public int Errors { get; init; }

        public double AverageResponseTimeMs { get; init; }
    }

    public sealed class DeveloperEndpointUsageVm
    {
        public string Endpoint { get; init; } = string.Empty;

        public int Requests { get; init; }

        public int Errors { get; init; }

        public double AverageResponseTimeMs { get; init; }
    }

    public sealed class DeveloperRecentRequestVm
    {
        public DateTime CreatedAtUtc { get; init; }

        public string ClientName { get; init; } = string.Empty;

        public string HttpMethod { get; init; } = string.Empty;

        public string Endpoint { get; init; } = string.Empty;

        public string ApiVersion { get; init; } = string.Empty;

        public int StatusCode { get; init; }

        public long ResponseTimeMs { get; init; }
    }
}