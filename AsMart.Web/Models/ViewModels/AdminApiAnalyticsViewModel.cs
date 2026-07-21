namespace AsMart.Web.Models.ViewModels
{
    public sealed class AdminApiAnalyticsViewModel
    {
        public DateTime FromUtc { get; init; }
        public DateTime ToUtc { get; init; }

        public long TotalRequests { get; init; }
        public long RequestsToday { get; init; }
        public long RequestsThisMonth { get; init; }

        public long SuccessfulRequests { get; init; }
        public long FailedRequests { get; init; }

        public double SuccessRate { get; init; }
        public double AverageResponseTimeMs { get; init; }

        public int TotalApiClients { get; init; }
        public int ActiveApiClients { get; init; }

        public long LegacyRequests { get; init; }
        public long V1Requests { get; init; }

        public IReadOnlyList<ApiAnalyticsDailyUsageVm> DailyUsage { get; init; }
            = Array.Empty<ApiAnalyticsDailyUsageVm>();

        public IReadOnlyList<ApiAnalyticsEndpointVm> TopEndpoints { get; init; }
            = Array.Empty<ApiAnalyticsEndpointVm>();

        public IReadOnlyList<ApiAnalyticsConsumerVm> TopConsumers { get; init; }
            = Array.Empty<ApiAnalyticsConsumerVm>();

        public IReadOnlyList<ApiAnalyticsStatusCodeVm> StatusCodes { get; init; }
            = Array.Empty<ApiAnalyticsStatusCodeVm>();

        public IReadOnlyList<ApiAnalyticsRecentErrorVm> RecentErrors { get; init; }
            = Array.Empty<ApiAnalyticsRecentErrorVm>();
    }

    public sealed class ApiAnalyticsDailyUsageVm
    {
        public DateTime DateUtc { get; init; }

        public int RequestCount { get; init; }

        public int SuccessCount { get; init; }

        public int ErrorCount { get; init; }

        public double AverageResponseTimeMs { get; init; }
    }

    public sealed class ApiAnalyticsEndpointVm
    {
        public string Endpoint { get; init; } = string.Empty;

        public int RequestCount { get; init; }

        public int ErrorCount { get; init; }

        public double AverageResponseTimeMs { get; init; }

        public long MaximumResponseTimeMs { get; init; }
    }

    public sealed class ApiAnalyticsConsumerVm
    {
        public int? ApiClientId { get; init; }

        public string ClientName { get; init; } = string.Empty;

        public string? Website { get; init; }

        public int RequestCount { get; init; }

        public int ErrorCount { get; init; }

        public double AverageResponseTimeMs { get; init; }

        public DateTime? LastRequestAtUtc { get; init; }
    }

    public sealed class ApiAnalyticsStatusCodeVm
    {
        public int StatusCode { get; init; }

        public int RequestCount { get; init; }
    }

    public sealed class ApiAnalyticsRecentErrorVm
    {
        public long Id { get; init; }

        public string ClientName { get; init; } = string.Empty;

        public string HttpMethod { get; init; } = string.Empty;

        public string Endpoint { get; init; } = string.Empty;

        public string ApiVersion { get; init; } = string.Empty;

        public int StatusCode { get; init; }

        public long ResponseTimeMs { get; init; }

        public string? IpAddress { get; init; }

        public DateTime CreatedAtUtc { get; init; }
    }
}