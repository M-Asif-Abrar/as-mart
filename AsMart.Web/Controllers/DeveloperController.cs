using System.Security.Claims;
using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using AsMart.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Controllers
{
    [Authorize]
    public sealed class DeveloperController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeveloperController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet("/Developer")]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return Challenge();
            }

            var utcNow = DateTime.UtcNow;
            var utcToday = utcNow.Date;

            var monthStartUtc = new DateTime(
                utcNow.Year,
                utcNow.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

            var nextMonthUtc = monthStartUtc.AddMonths(1);

            var chartStartUtc = utcToday.AddDays(-29);

            var clients = await _db.ApiClients
                .AsNoTracking()
                .Where(client => client.UserId == userId)
                .OrderByDescending(client => client.CreatedAt)
                .ToListAsync(cancellationToken);

            var clientIds = clients
                .Select(client => client.Id)
                .ToList();

            var monthlyLogs = _db.ApiUsageLogs
                .AsNoTracking()
                .Where(log =>
                    log.ApiClientId.HasValue &&
                    clientIds.Contains(log.ApiClientId.Value) &&
                    log.CreatedAt >= monthStartUtc &&
                    log.CreatedAt < nextMonthUtc);

            var requestsThisMonth = clientIds.Count == 0
                ? 0
                : await monthlyLogs.LongCountAsync(
                    cancellationToken);

            var successfulRequestsThisMonth = clientIds.Count == 0
                ? 0
                : await monthlyLogs.LongCountAsync(
                    log =>
                        log.StatusCode >= 200 &&
                        log.StatusCode < 400,
                    cancellationToken);

            var failedRequestsThisMonth = clientIds.Count == 0
                ? 0
                : await monthlyLogs.LongCountAsync(
                    log => log.StatusCode >= 400,
                    cancellationToken);

            var requestsToday = clientIds.Count == 0
                ? 0
                : await _db.ApiUsageLogs
                    .AsNoTracking()
                    .LongCountAsync(
                        log =>
                            log.ApiClientId.HasValue &&
                            clientIds.Contains(log.ApiClientId.Value) &&
                            log.CreatedAt >= utcToday,
                        cancellationToken);

            var averageResponseTimeMs =
                requestsThisMonth > 0
                    ? await monthlyLogs.AverageAsync(
                        log => (double)log.ResponseTimeMs,
                        cancellationToken)
                    : 0;

            var usageByClient = clientIds.Count == 0
                ? new Dictionary<int, long>()
                : await monthlyLogs
                    .GroupBy(log => log.ApiClientId!.Value)
                    .Select(group => new
                    {
                        ClientId = group.Key,
                        Requests = group.LongCount()
                    })
                    .ToDictionaryAsync(
                        item => item.ClientId,
                        item => item.Requests,
                        cancellationToken);

            var apiKeySummaries = clients
                .Select(client =>
                {
                    usageByClient.TryGetValue(
                        client.Id,
                        out var usage);

                    var remainingQuota =
                        client.MonthlyQuota <= 0
                            ? long.MaxValue
                            : Math.Max(
                                client.MonthlyQuota - usage,
                                0);

                    return new DeveloperApiKeySummaryVm
                    {
                        Id = client.Id,
                        Name = client.Name,
                        MaskedApiKey = client.MaskedApiKey,
                        Website = client.Website,
                        Status = client.LifecycleStatus,
                        IsUsable = client.IsUsable,
                        RateLimitPerMinute =
                            client.RateLimitPerMinute,
                        MonthlyQuota = client.MonthlyQuota,
                        RequestsThisMonth = usage,
                        RemainingQuota = remainingQuota,
                        CreatedAtUtc = client.CreatedAt,
                        LastUsedAtUtc = client.LastUsedAt,
                        ExpiresAtUtc = client.ExpiresAt
                    };
                })
                .ToList();

            var totalMonthlyQuota = clients
                .Where(client => client.MonthlyQuota > 0)
                .Sum(client => (long)client.MonthlyQuota);

            var hasUnlimitedQuota = clients.Any(
                client => client.MonthlyQuota <= 0);

            var remainingMonthlyQuota = hasUnlimitedQuota
                ? long.MaxValue
                : Math.Max(
                    totalMonthlyQuota - requestsThisMonth,
                    0);

            var quotaUsagePercentage =
                hasUnlimitedQuota ||
                totalMonthlyQuota <= 0
                    ? 0
                    : Math.Min(
                        requestsThisMonth * 100d /
                        totalMonthlyQuota,
                        100);

            var dailyUsageRaw = clientIds.Count == 0
                ? []
                : await _db.ApiUsageLogs
                    .AsNoTracking()
                    .Where(log =>
                        log.ApiClientId.HasValue &&
                        clientIds.Contains(log.ApiClientId.Value) &&
                        log.CreatedAt >= chartStartUtc)
                    .GroupBy(log => log.CreatedAt.Date)
                    .Select(group => new
                    {
                        DateUtc = group.Key,
                        Requests = group.Count(),

                        Errors = group.Count(
                            log => log.StatusCode >= 400),

                        AverageResponseTimeMs =
                            group.Average(
                                log =>
                                    (double)log.ResponseTimeMs)
                    })
                    .OrderBy(item => item.DateUtc)
                    .ToListAsync(cancellationToken);

            var dailyUsageMap = dailyUsageRaw.ToDictionary(
                item => item.DateUtc.Date);

            var dailyUsage = Enumerable.Range(0, 30)
                .Select(dayOffset =>
                {
                    var date = chartStartUtc.AddDays(dayOffset);

                    if (!dailyUsageMap.TryGetValue(
                            date.Date,
                            out var value))
                    {
                        return new DeveloperDailyUsageVm
                        {
                            DateUtc = date,
                            Requests = 0,
                            Errors = 0,
                            AverageResponseTimeMs = 0
                        };
                    }

                    return new DeveloperDailyUsageVm
                    {
                        DateUtc = value.DateUtc,
                        Requests = value.Requests,
                        Errors = value.Errors,
                        AverageResponseTimeMs =
                            Math.Round(
                                value.AverageResponseTimeMs,
                                2)
                    };
                })
                .ToList();

            var topEndpoints = clientIds.Count == 0
                ? []
                : await monthlyLogs
                    .GroupBy(log => log.Endpoint)
                    .Select(group =>
                        new DeveloperEndpointUsageVm
                        {
                            Endpoint = group.Key,
                            Requests = group.Count(),

                            Errors = group.Count(
                                log =>
                                    log.StatusCode >= 400),

                            AverageResponseTimeMs =
                                group.Average(
                                    log =>
                                        (double)log.ResponseTimeMs)
                        })
                    .OrderByDescending(item => item.Requests)
                    .Take(10)
                    .ToListAsync(cancellationToken);

            var recentRequests = clientIds.Count == 0
                ? []
                : await _db.ApiUsageLogs
                    .AsNoTracking()
                    .Where(log =>
                        log.ApiClientId.HasValue &&
                        clientIds.Contains(log.ApiClientId.Value))
                    .OrderByDescending(log => log.CreatedAt)
                    .Take(30)
                    .Select(log =>
                        new DeveloperRecentRequestVm
                        {
                            CreatedAtUtc = log.CreatedAt,

                            ClientName =
                                log.ApiClient != null
                                    ? log.ApiClient.Name
                                    : "Unknown",

                            HttpMethod = log.HttpMethod,
                            Endpoint = log.Endpoint,
                            ApiVersion = log.ApiVersion,
                            StatusCode = log.StatusCode,
                            ResponseTimeMs =
                                log.ResponseTimeMs
                        })
                    .ToListAsync(cancellationToken);

            var model = new DeveloperDashboardViewModel
            {
                DisplayName = user.DisplayName,
                Email = user.Email ?? string.Empty,

                TotalApiKeys = clients.Count,

                ActiveApiKeys = clients.Count(
                    client => client.IsUsable),

                ExpiredApiKeys = clients.Count(
                    client => client.IsExpired),

                RevokedApiKeys = clients.Count(
                    client => client.IsRevoked),

                RequestsToday = requestsToday,
                RequestsThisMonth = requestsThisMonth,

                SuccessfulRequestsThisMonth =
                    successfulRequestsThisMonth,

                FailedRequestsThisMonth =
                    failedRequestsThisMonth,

                SuccessRate =
                    requestsThisMonth > 0
                        ? Math.Round(
                            successfulRequestsThisMonth * 100d /
                            requestsThisMonth,
                            2)
                        : 0,

                AverageResponseTimeMs =
                    Math.Round(
                        averageResponseTimeMs,
                        2),

                TotalMonthlyQuota = hasUnlimitedQuota
                    ? long.MaxValue
                    : totalMonthlyQuota,

                RemainingMonthlyQuota =
                    remainingMonthlyQuota,

                QuotaUsagePercentage =
                    Math.Round(
                        quotaUsagePercentage,
                        2),

                ApiKeys = apiKeySummaries,
                DailyUsage = dailyUsage,
                TopEndpoints = topEndpoints,
                RecentRequests = recentRequests
            };

            return View(model);
        }
    }
}