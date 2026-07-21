using AsMart.Web.Data;
using AsMart.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public sealed class ApiAnalyticsController : Controller
    {
        private const int MaximumRangeDays = 365;

        private readonly ApplicationDbContext _db;

        public ApiAnalyticsController(
            ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? from,
            DateTime? to,
            CancellationToken cancellationToken)
        {
            var utcToday = DateTime.UtcNow.Date;

            var toUtc = NormalizeToUtc(to, utcToday);
            var fromUtc = NormalizeFromUtc(from, toUtc);

            var logs = _db.ApiUsageLogs
                .AsNoTracking()
                .Where(log =>
                    log.CreatedAt >= fromUtc &&
                    log.CreatedAt < toUtc);

            var totalRequests =
                await logs.LongCountAsync(cancellationToken);

            var successfulRequests =
                await logs.LongCountAsync(
                    log =>
                        log.StatusCode >= 200 &&
                        log.StatusCode < 400,
                    cancellationToken);

            var failedRequests =
                await logs.LongCountAsync(
                    log => log.StatusCode >= 400,
                    cancellationToken);

            var requestsToday =
                await _db.ApiUsageLogs
                    .AsNoTracking()
                    .LongCountAsync(
                        log => log.CreatedAt >= utcToday,
                        cancellationToken);

            var monthStartUtc = new DateTime(
                utcToday.Year,
                utcToday.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

            var requestsThisMonth =
                await _db.ApiUsageLogs
                    .AsNoTracking()
                    .LongCountAsync(
                        log => log.CreatedAt >= monthStartUtc,
                        cancellationToken);

            var averageResponseTime =
                totalRequests > 0
                    ? await logs.AverageAsync(
                        log => (double)log.ResponseTimeMs,
                        cancellationToken)
                    : 0;

            var totalApiClients =
                await _db.ApiClients
                    .AsNoTracking()
                    .CountAsync(cancellationToken);

            var nowUtc = DateTime.UtcNow;

            var activeApiClients =
                await _db.ApiClients
                    .AsNoTracking()
                    .CountAsync(
                        client =>
                            client.IsActive &&
                            client.RevokedAt == null &&
                            (
                                client.ExpiresAt == null ||
                                client.ExpiresAt > nowUtc
                            ),
                        cancellationToken);

            var versionCounts =
                await logs
                    .GroupBy(log => log.ApiVersion)
                    .Select(group => new
                    {
                        ApiVersion = group.Key,
                        Count = group.LongCount()
                    })
                    .ToListAsync(cancellationToken);

            var legacyRequests = versionCounts
                .Where(item => item.ApiVersion == "legacy")
                .Sum(item => item.Count);

            var v1Requests = versionCounts
                .Where(item => item.ApiVersion == "v1")
                .Sum(item => item.Count);

            var dailyUsageRaw =
                await logs
                    .GroupBy(log => log.CreatedAt.Date)
                    .Select(group => new
                    {
                        DateUtc = group.Key,
                        RequestCount = group.Count(),

                        SuccessCount = group.Count(
                            log =>
                                log.StatusCode >= 200 &&
                                log.StatusCode < 400),

                        ErrorCount = group.Count(
                            log => log.StatusCode >= 400),

                        AverageResponseTimeMs =
                            group.Average(
                                log =>
                                    (double)log.ResponseTimeMs)
                    })
                    .OrderBy(item => item.DateUtc)
                    .ToListAsync(cancellationToken);

            var dailyUsage = dailyUsageRaw
                .Select(item => new ApiAnalyticsDailyUsageVm
                {
                    DateUtc = item.DateUtc,
                    RequestCount = item.RequestCount,
                    SuccessCount = item.SuccessCount,
                    ErrorCount = item.ErrorCount,
                    AverageResponseTimeMs =
                        Math.Round(
                            item.AverageResponseTimeMs,
                            2)
                })
                .ToList();

            var topEndpointsRaw =
                await logs
                    .GroupBy(log => log.Endpoint)
                    .Select(group => new
                    {
                        Endpoint = group.Key,
                        RequestCount = group.Count(),

                        ErrorCount = group.Count(
                            log => log.StatusCode >= 400),

                        AverageResponseTimeMs =
                            group.Average(
                                log =>
                                    (double)log.ResponseTimeMs),

                        MaximumResponseTimeMs =
                            group.Max(
                                log => log.ResponseTimeMs)
                    })
                    .OrderByDescending(item => item.RequestCount)
                    .Take(15)
                    .ToListAsync(cancellationToken);

            var topEndpoints = topEndpointsRaw
                .Select(item => new ApiAnalyticsEndpointVm
                {
                    Endpoint = item.Endpoint,
                    RequestCount = item.RequestCount,
                    ErrorCount = item.ErrorCount,
                    AverageResponseTimeMs =
                        Math.Round(
                            item.AverageResponseTimeMs,
                            2),
                    MaximumResponseTimeMs =
                        item.MaximumResponseTimeMs
                })
                .ToList();

            var topConsumersRaw =
                await logs
                    .GroupBy(log => new
                    {
                        log.ApiClientId,
                        ClientName =
                            log.ApiClient != null
                                ? log.ApiClient.Name
                                : "Anonymous",

                        Website =
                            log.ApiClient != null
                                ? log.ApiClient.Website
                                : null
                    })
                    .Select(group => new
                    {
                        group.Key.ApiClientId,
                        group.Key.ClientName,
                        group.Key.Website,

                        RequestCount = group.Count(),

                        ErrorCount = group.Count(
                            log => log.StatusCode >= 400),

                        AverageResponseTimeMs =
                            group.Average(
                                log =>
                                    (double)log.ResponseTimeMs),

                        LastRequestAtUtc =
                            group.Max(
                                log =>
                                    (DateTime?)log.CreatedAt)
                    })
                    .OrderByDescending(item => item.RequestCount)
                    .Take(15)
                    .ToListAsync(cancellationToken);

            var topConsumers = topConsumersRaw
                .Select(item => new ApiAnalyticsConsumerVm
                {
                    ApiClientId = item.ApiClientId,
                    ClientName = item.ClientName,
                    Website = item.Website,
                    RequestCount = item.RequestCount,
                    ErrorCount = item.ErrorCount,
                    AverageResponseTimeMs =
                        Math.Round(
                            item.AverageResponseTimeMs,
                            2),
                    LastRequestAtUtc =
                        item.LastRequestAtUtc
                })
                .ToList();

            var statusCodes =
                await logs
                    .GroupBy(log => log.StatusCode)
                    .Select(group =>
                        new ApiAnalyticsStatusCodeVm
                        {
                            StatusCode = group.Key,
                            RequestCount = group.Count()
                        })
                    .OrderBy(item => item.StatusCode)
                    .ToListAsync(cancellationToken);

            var recentErrors =
                await logs
                    .Where(log => log.StatusCode >= 400)
                    .OrderByDescending(log => log.CreatedAt)
                    .Take(50)
                    .Select(log =>
                        new ApiAnalyticsRecentErrorVm
                        {
                            Id = log.Id,

                            ClientName =
                                log.ApiClient != null
                                    ? log.ApiClient.Name
                                    : "Anonymous",

                            HttpMethod = log.HttpMethod,
                            Endpoint = log.Endpoint,
                            ApiVersion = log.ApiVersion,
                            StatusCode = log.StatusCode,
                            ResponseTimeMs =
                                log.ResponseTimeMs,
                            IpAddress = log.IpAddress,
                            CreatedAtUtc = log.CreatedAt
                        })
                    .ToListAsync(cancellationToken);

            var model =
                new AdminApiAnalyticsViewModel
                {
                    FromUtc = fromUtc,
                    ToUtc = toUtc,

                    TotalRequests = totalRequests,
                    RequestsToday = requestsToday,
                    RequestsThisMonth =
                        requestsThisMonth,

                    SuccessfulRequests =
                        successfulRequests,
                    FailedRequests = failedRequests,

                    SuccessRate =
                        totalRequests > 0
                            ? Math.Round(
                                successfulRequests * 100d /
                                totalRequests,
                                2)
                            : 0,

                    AverageResponseTimeMs =
                        Math.Round(
                            averageResponseTime,
                            2),

                    TotalApiClients = totalApiClients,
                    ActiveApiClients = activeApiClients,

                    LegacyRequests = legacyRequests,
                    V1Requests = v1Requests,

                    DailyUsage = dailyUsage,
                    TopEndpoints = topEndpoints,
                    TopConsumers = topConsumers,
                    StatusCodes = statusCodes,
                    RecentErrors = recentErrors
                };

            return View(model);
        }

        private static DateTime NormalizeToUtc(
            DateTime? to,
            DateTime utcToday)
        {
            var toDate = to?.Date ?? utcToday;

            if (toDate > utcToday)
            {
                toDate = utcToday;
            }

            return DateTime.SpecifyKind(
                toDate.AddDays(1),
                DateTimeKind.Utc);
        }

        private static DateTime NormalizeFromUtc(
            DateTime? from,
            DateTime toUtc)
        {
            var defaultFrom =
                toUtc.AddDays(-30).Date;

            var fromDate =
                from?.Date ?? defaultFrom;

            var minimumFrom =
                toUtc.AddDays(-MaximumRangeDays).Date;

            if (fromDate < minimumFrom)
            {
                fromDate = minimumFrom;
            }

            if (fromDate >= toUtc)
            {
                fromDate = toUtc.AddDays(-30).Date;
            }

            return DateTime.SpecifyKind(
                fromDate,
                DateTimeKind.Utc);
        }
    }
}