namespace AsMart.Web.Models.ViewModels
{
    public sealed class DeveloperApplicationDetailsViewModel
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Website { get; init; }

        public string? Notes { get; init; }

        public string MaskedApiKey { get; init; } = string.Empty;

        public string ApiKeyPrefix { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public bool IsActive { get; init; }

        public bool IsUsable { get; init; }

        public bool IsExpired { get; init; }

        public bool IsRevoked { get; init; }

        public int RateLimitPerMinute { get; init; }

        public int MonthlyQuota { get; init; }

        public bool HasUnlimitedQuota => MonthlyQuota <= 0;

        public long RequestsToday { get; init; }

        public long RequestsThisMonth { get; init; }

        public long SuccessfulRequestsThisMonth { get; init; }

        public long FailedRequestsThisMonth { get; init; }

        public long RemainingQuota { get; init; }

        public double QuotaUsagePercentage { get; init; }

        public double SuccessRate { get; init; }

        public double AverageResponseTimeMs { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public DateTime? LastUsedAtUtc { get; init; }

        public DateTime? ExpiresAtUtc { get; init; }

        public DateTime? RevokedAtUtc { get; init; }

        public DateTime? LastRotatedAtUtc { get; init; }

        public IReadOnlyList<DeveloperApplicationEndpointVm>
            TopEndpoints
        { get; init; }
                = Array.Empty<DeveloperApplicationEndpointVm>();

        public IReadOnlyList<DeveloperApplicationRecentRequestVm>
            RecentRequests
        { get; init; }
                = Array.Empty<DeveloperApplicationRecentRequestVm>();
    }

    public sealed class DeveloperApplicationEndpointVm
    {
        public string Endpoint { get; init; } = string.Empty;

        public int Requests { get; init; }

        public int Errors { get; init; }

        public double AverageResponseTimeMs { get; init; }
    }

    public sealed class DeveloperApplicationRecentRequestVm
    {
        public DateTime CreatedAtUtc { get; init; }

        public string HttpMethod { get; init; } = string.Empty;

        public string Endpoint { get; init; } = string.Empty;

        public string ApiVersion { get; init; } = string.Empty;

        public int StatusCode { get; init; }

        public long ResponseTimeMs { get; init; }

        public string? IpAddress { get; init; }
    }
}