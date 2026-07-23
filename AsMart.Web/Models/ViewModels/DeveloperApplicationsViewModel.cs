namespace AsMart.Web.Models.ViewModels
{
    public sealed class DeveloperApplicationsViewModel
    {
        public int TotalApplications { get; init; }

        public int ActiveApplications { get; init; }

        public int DisabledApplications { get; init; }

        public int ExpiredApplications { get; init; }

        public int RevokedApplications { get; init; }

        public long RequestsThisMonth { get; init; }

        public string SearchTerm { get; init; } = string.Empty;

        public string StatusFilter { get; init; } = "all";

        public int PageNumber { get; init; } = 1;

        public int PageSize { get; init; } = 12;

        public int TotalFilteredApplications { get; init; }

        public int TotalPages =>
            TotalFilteredApplications <= 0
                ? 1
                : (int)Math.Ceiling(
                    TotalFilteredApplications /
                    (double)Math.Max(PageSize, 1));

        public bool HasPreviousPage =>
            PageNumber > 1;

        public bool HasNextPage =>
            PageNumber < TotalPages;

        public IReadOnlyList<DeveloperApplicationListItemVm>
            Applications
        { get; init; }
                = Array.Empty<DeveloperApplicationListItemVm>();
    }

    public sealed class DeveloperApplicationListItemVm
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Website { get; init; }

        public string MaskedApiKey { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public bool IsUsable { get; init; }

        public int RateLimitPerMinute { get; init; }

        public int MonthlyQuota { get; init; }

        public long RequestsThisMonth { get; init; }

        public long RemainingQuota { get; init; }

        public double QuotaUsagePercentage { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public DateTime? LastUsedAtUtc { get; init; }

        public DateTime? ExpiresAtUtc { get; init; }
    }
}