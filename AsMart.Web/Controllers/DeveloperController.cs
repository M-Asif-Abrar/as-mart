using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using AsMart.Web.Models.ViewModels;
using AsMart.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Globalization;
using System.Text;

namespace AsMart.Web.Controllers
{
    [Authorize]
    public sealed class DeveloperController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApiKeyService _apiKeyService;
        public DeveloperController(
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                IApiKeyService apiKeyService)
        {
            _db = db;
            _userManager = userManager;
            _apiKeyService = apiKeyService;
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

        [HttpGet("/Developer/Applications")]
        public async Task<IActionResult> Applications(
    [FromQuery] string? search,
    [FromQuery] string? status = "all",
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 12,
    CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            search = search?.Trim();

            status = string.IsNullOrWhiteSpace(status)
                ? "all"
                : status.Trim().ToLowerInvariant();

            status = status switch
            {
                "all" => "all",
                "active" => "active",
                "disabled" => "disabled",
                "expired" => "expired",
                "revoked" => "revoked",
                _ => "all"
            };

            page = Math.Max(page, 1);

            pageSize = pageSize switch
            {
                12 => 12,
                24 => 24,
                48 => 48,
                _ => 12
            };

            var utcNow = DateTime.UtcNow;

            var monthStartUtc = new DateTime(
                utcNow.Year,
                utcNow.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

            var nextMonthUtc =
                monthStartUtc.AddMonths(1);

            var allClientsQuery = _db.ApiClients
                .AsNoTracking()
                .Where(client => client.UserId == userId);

            var totalApplications =
                await allClientsQuery.CountAsync(
                    cancellationToken);

            var activeApplications =
                await allClientsQuery.CountAsync(
                    client =>
                        client.IsActive &&
                        !client.RevokedAt.HasValue &&
                        (
                            !client.ExpiresAt.HasValue ||
                            client.ExpiresAt > utcNow
                        ),
                    cancellationToken);

            var disabledApplications =
                await allClientsQuery.CountAsync(
                    client =>
                        !client.IsActive &&
                        !client.RevokedAt.HasValue &&
                        (
                            !client.ExpiresAt.HasValue ||
                            client.ExpiresAt > utcNow
                        ),
                    cancellationToken);

            var expiredApplications =
                await allClientsQuery.CountAsync(
                    client =>
                        client.ExpiresAt.HasValue &&
                        client.ExpiresAt <= utcNow,
                    cancellationToken);

            var revokedApplications =
                await allClientsQuery.CountAsync(
                    client => client.RevokedAt.HasValue,
                    cancellationToken);

            var filteredQuery =
                allClientsQuery;

            if (!string.IsNullOrWhiteSpace(search))
            {
                filteredQuery = filteredQuery.Where(
                    client =>
                        client.Name.Contains(search) ||
                        (
                            client.Website != null &&
                            client.Website.Contains(search)
                        ) ||
                        client.ApiKeyPrefix.Contains(search));
            }

            filteredQuery = status switch
            {
                "active" => filteredQuery.Where(
                    client =>
                        client.IsActive &&
                        !client.RevokedAt.HasValue &&
                        (
                            !client.ExpiresAt.HasValue ||
                            client.ExpiresAt > utcNow
                        )),

                "disabled" => filteredQuery.Where(
                    client =>
                        !client.IsActive &&
                        !client.RevokedAt.HasValue &&
                        (
                            !client.ExpiresAt.HasValue ||
                            client.ExpiresAt > utcNow
                        )),

                "expired" => filteredQuery.Where(
                    client =>
                        client.ExpiresAt.HasValue &&
                        client.ExpiresAt <= utcNow),

                "revoked" => filteredQuery.Where(
                    client => client.RevokedAt.HasValue),

                _ => filteredQuery
            };

            var totalFilteredApplications =
                await filteredQuery.CountAsync(
                    cancellationToken);

            var totalPages = Math.Max(
                1,
                (int)Math.Ceiling(
                    totalFilteredApplications /
                    (double)pageSize));

            page = Math.Min(
                page,
                totalPages);

            var clients = await filteredQuery
                .OrderByDescending(client => client.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var clientIds = clients
                .Select(client => client.Id)
                .ToList();

            var monthlyUsage =
                clientIds.Count == 0
                    ? new Dictionary<int, long>()
                    : await _db.ApiUsageLogs
                        .AsNoTracking()
                        .Where(log =>
                            log.ApiClientId.HasValue &&
                            clientIds.Contains(
                                log.ApiClientId.Value) &&
                            log.CreatedAt >= monthStartUtc &&
                            log.CreatedAt < nextMonthUtc)
                        .GroupBy(log =>
                            log.ApiClientId!.Value)
                        .Select(group => new
                        {
                            ApiClientId = group.Key,
                            Requests = group.LongCount()
                        })
                        .ToDictionaryAsync(
                            item => item.ApiClientId,
                            item => item.Requests,
                            cancellationToken);

            var totalMonthlyRequests =
                await _db.ApiUsageLogs
                    .AsNoTracking()
                    .LongCountAsync(
                        log =>
                            log.ApiClientId.HasValue &&
                            _db.ApiClients.Any(client =>
                                client.Id ==
                                    log.ApiClientId.Value &&
                                client.UserId == userId) &&
                            log.CreatedAt >= monthStartUtc &&
                            log.CreatedAt < nextMonthUtc,
                        cancellationToken);

            var applications = clients
                .Select(client =>
                {
                    monthlyUsage.TryGetValue(
                        client.Id,
                        out var requestsThisMonth);

                    var hasUnlimitedQuota =
                        client.MonthlyQuota <= 0;

                    var remainingQuota =
                        hasUnlimitedQuota
                            ? long.MaxValue
                            : Math.Max(
                                client.MonthlyQuota -
                                requestsThisMonth,
                                0);

                    var quotaUsagePercentage =
                        hasUnlimitedQuota
                            ? 0
                            : Math.Min(
                                requestsThisMonth * 100d /
                                Math.Max(
                                    client.MonthlyQuota,
                                    1),
                                100);

                    return new DeveloperApplicationListItemVm
                    {
                        Id = client.Id,
                        Name = client.Name,
                        Website = client.Website,
                        MaskedApiKey = client.MaskedApiKey,
                        Status = client.LifecycleStatus,
                        IsUsable = client.IsUsable,

                        RateLimitPerMinute =
                            client.RateLimitPerMinute,

                        MonthlyQuota =
                            client.MonthlyQuota,

                        RequestsThisMonth =
                            requestsThisMonth,

                        RemainingQuota =
                            remainingQuota,

                        QuotaUsagePercentage =
                            Math.Round(
                                quotaUsagePercentage,
                                2),

                        CreatedAtUtc =
                            client.CreatedAt,

                        LastUsedAtUtc =
                            client.LastUsedAt,

                        ExpiresAtUtc =
                            client.ExpiresAt
                    };
                })
                .ToList();

            var model =
                new DeveloperApplicationsViewModel
                {
                    TotalApplications =
                        totalApplications,

                    ActiveApplications =
                        activeApplications,

                    DisabledApplications =
                        disabledApplications,

                    ExpiredApplications =
                        expiredApplications,

                    RevokedApplications =
                        revokedApplications,

                    RequestsThisMonth =
                        totalMonthlyRequests,

                    SearchTerm =
                        search ?? string.Empty,

                    StatusFilter =
                        status,

                    PageNumber =
                        page,

                    PageSize =
                        pageSize,

                    TotalFilteredApplications =
                        totalFilteredApplications,

                    Applications =
                        applications
                };

            return View(model);
        }


        [HttpGet("/Developer/Applications/{id:int}")]
        public async Task<IActionResult> ApplicationDetails(
        int id,
        CancellationToken cancellationToken)
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Challenge();
                }

                var client = await _db.ApiClients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id == id &&
                            item.UserId == userId,
                        cancellationToken);

                if (client is null)
                {
                    return NotFound();
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

                var monthlyLogs = _db.ApiUsageLogs
                    .AsNoTracking()
                    .Where(log =>
                        log.ApiClientId == client.Id &&
                        log.CreatedAt >= monthStartUtc &&
                        log.CreatedAt < nextMonthUtc);

                var requestsThisMonth =
                    await monthlyLogs.LongCountAsync(
                        cancellationToken);

                var successfulRequestsThisMonth =
                    await monthlyLogs.LongCountAsync(
                        log =>
                            log.StatusCode >= 200 &&
                            log.StatusCode < 400,
                        cancellationToken);

                var failedRequestsThisMonth =
                    await monthlyLogs.LongCountAsync(
                        log => log.StatusCode >= 400,
                        cancellationToken);

                var requestsToday = await _db.ApiUsageLogs
                    .AsNoTracking()
                    .LongCountAsync(
                        log =>
                            log.ApiClientId == client.Id &&
                            log.CreatedAt >= utcToday,
                        cancellationToken);

                var averageResponseTimeMs =
                    requestsThisMonth > 0
                        ? await monthlyLogs.AverageAsync(
                            log => (double)log.ResponseTimeMs,
                            cancellationToken)
                        : 0;

                var remainingQuota =
                    client.MonthlyQuota <= 0
                        ? long.MaxValue
                        : Math.Max(
                            client.MonthlyQuota - requestsThisMonth,
                            0);

                var quotaUsagePercentage =
                    client.MonthlyQuota <= 0
                        ? 0
                        : Math.Min(
                            requestsThisMonth * 100d /
                            Math.Max(client.MonthlyQuota, 1),
                            100);

                var successRate =
                    requestsThisMonth > 0
                        ? successfulRequestsThisMonth * 100d /
                          requestsThisMonth
                        : 0;

                var topEndpointsRaw = await monthlyLogs
                    .GroupBy(log => log.Endpoint)
                    .Select(group => new
                    {
                        Endpoint = group.Key,
                        Requests = group.Count(),

                        Errors = group.Count(
                            log => log.StatusCode >= 400),

                        AverageResponseTimeMs =
                            group.Average(
                                log => (double)log.ResponseTimeMs)
                    })
                    .OrderByDescending(item => item.Requests)
                    .Take(10)
                    .ToListAsync(cancellationToken);

                var topEndpoints = topEndpointsRaw
                    .Select(item => new DeveloperApplicationEndpointVm
                    {
                        Endpoint = item.Endpoint,
                        Requests = item.Requests,
                        Errors = item.Errors,

                        AverageResponseTimeMs = Math.Round(
                            item.AverageResponseTimeMs,
                            2)
                    })
                    .ToList();

                var recentRequests = await _db.ApiUsageLogs
                    .AsNoTracking()
                    .Where(log => log.ApiClientId == client.Id)
                    .OrderByDescending(log => log.CreatedAt)
                    .Take(25)
                    .Select(log =>
                        new DeveloperApplicationRecentRequestVm
                        {
                            CreatedAtUtc = log.CreatedAt,
                            HttpMethod = log.HttpMethod,
                            Endpoint = log.Endpoint,
                            ApiVersion = log.ApiVersion,
                            StatusCode = log.StatusCode,
                            ResponseTimeMs = log.ResponseTimeMs,
                            IpAddress = log.IpAddress
                        })
                    .ToListAsync(cancellationToken);

                var model =
                    new DeveloperApplicationDetailsViewModel
                    {
                        Id = client.Id,
                        Name = client.Name,
                        Website = client.Website,
                        Notes = client.Notes,
                        MaskedApiKey = client.MaskedApiKey,
                        ApiKeyPrefix = client.ApiKeyPrefix,
                        Status = client.LifecycleStatus,

                        IsActive = client.IsActive,
                        IsUsable = client.IsUsable,
                        IsExpired = client.IsExpired,
                        IsRevoked = client.IsRevoked,

                        RateLimitPerMinute =
                            client.RateLimitPerMinute,

                        MonthlyQuota =
                            client.MonthlyQuota,

                        RequestsToday =
                            requestsToday,

                        RequestsThisMonth =
                            requestsThisMonth,

                        SuccessfulRequestsThisMonth =
                            successfulRequestsThisMonth,

                        FailedRequestsThisMonth =
                            failedRequestsThisMonth,

                        RemainingQuota =
                            remainingQuota,

                        QuotaUsagePercentage =
                            Math.Round(
                                quotaUsagePercentage,
                                2),

                        SuccessRate =
                            Math.Round(
                                successRate,
                                2),

                        AverageResponseTimeMs =
                            Math.Round(
                                averageResponseTimeMs,
                                2),

                        CreatedAtUtc =
                            client.CreatedAt,

                        LastUsedAtUtc =
                            client.LastUsedAt,

                        ExpiresAtUtc =
                            client.ExpiresAt,

                        RevokedAtUtc =
                            client.RevokedAt,

                        LastRotatedAtUtc =
                            client.LastRotatedAt,

                        TopEndpoints =
                            topEndpoints,

                        RecentRequests =
                            recentRequests
                    };

                return View(model);
            }


        [HttpGet("/Developer/Applications/{id:int}/Usage")]
        public async Task<IActionResult> ApplicationUsage(
        int id,
        [FromQuery] int days = 30,
        [FromQuery] string? endpoint = null,
        [FromQuery] string? method = "all",
        [FromQuery] string? requestStatus = "all",
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            days = days switch
            {
                7 => 7,
                30 => 30,
                90 => 90,
                _ => 30
            };

            endpoint = endpoint?.Trim();

            method = string.IsNullOrWhiteSpace(method)
                ? "all"
                : method.Trim().ToUpperInvariant();

            method = method switch
            {
                "GET" => "GET",
                "POST" => "POST",
                "PUT" => "PUT",
                "PATCH" => "PATCH",
                "DELETE" => "DELETE",
                "HEAD" => "HEAD",
                "OPTIONS" => "OPTIONS",
                _ => "all"
            };

            requestStatus = string.IsNullOrWhiteSpace(requestStatus)
                ? "all"
                : requestStatus.Trim().ToLowerInvariant();

            requestStatus = requestStatus switch
            {
                "success" => "success",
                "redirect" => "redirect",
                "client-error" => "client-error",
                "server-error" => "server-error",
                "error" => "error",
                _ => "all"
            };

            page = Math.Max(page, 1);

            pageSize = pageSize switch
            {
                25 => 25,
                50 => 50,
                100 => 100,
                _ => 25
            };

            var application = await _db.ApiClients
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    client =>
                        client.Id == id &&
                        client.UserId == userId,
                    cancellationToken);

            if (application is null)
            {
                return NotFound();
            }

            var utcNow = DateTime.UtcNow;
            var periodEndUtc = utcNow;
            var periodStartUtc =
                utcNow.Date.AddDays(-(days - 1));

            var monthStartUtc = new DateTime(
                utcNow.Year,
                utcNow.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

            var nextMonthUtc =
                monthStartUtc.AddMonths(1);

            // Analytics always use the selected 7/30/90-day period.
            var usageQuery = _db.ApiUsageLogs
                .AsNoTracking()
                .Where(log =>
                    log.ApiClientId == application.Id &&
                    log.CreatedAt >= periodStartUtc &&
                    log.CreatedAt <= periodEndUtc);

            var totalRequests =
                await usageQuery.LongCountAsync(
                    cancellationToken);

            var successfulRequests =
                await usageQuery.LongCountAsync(
                    log =>
                        log.StatusCode >= 200 &&
                        log.StatusCode < 400,
                    cancellationToken);

            var failedRequests =
                await usageQuery.LongCountAsync(
                    log => log.StatusCode >= 400,
                    cancellationToken);

            var averageResponseTimeMs =
                totalRequests == 0
                    ? 0
                    : await usageQuery.AverageAsync(
                        log => (double)log.ResponseTimeMs,
                        cancellationToken);

            var requestsToday =
                await _db.ApiUsageLogs
                    .AsNoTracking()
                    .LongCountAsync(
                        log =>
                            log.ApiClientId == application.Id &&
                            log.CreatedAt >= utcNow.Date,
                        cancellationToken);

            var requestsThisMonth =
                await _db.ApiUsageLogs
                    .AsNoTracking()
                    .LongCountAsync(
                        log =>
                            log.ApiClientId == application.Id &&
                            log.CreatedAt >= monthStartUtc &&
                            log.CreatedAt < nextMonthUtc,
                        cancellationToken);

            var remainingQuota =
                application.MonthlyQuota <= 0
                    ? long.MaxValue
                    : Math.Max(
                        application.MonthlyQuota -
                        requestsThisMonth,
                        0);

            var groupedDailyUsage =
                await usageQuery
                    .GroupBy(log => log.CreatedAt.Date)
                    .Select(group => new
                    {
                        DateUtc = group.Key,
                        Requests = group.LongCount(),

                        SuccessfulRequests =
                            group.LongCount(
                                log =>
                                    log.StatusCode >= 200 &&
                                    log.StatusCode < 400),

                        FailedRequests =
                            group.LongCount(
                                log => log.StatusCode >= 400),

                        AverageResponseTimeMs =
                            group.Average(
                                log =>
                                    (double)log.ResponseTimeMs)
                    })
                    .OrderBy(item => item.DateUtc)
                    .ToListAsync(cancellationToken);

            var dailyLookup =
                groupedDailyUsage.ToDictionary(
                    item => item.DateUtc.Date);

            var dailyUsage = Enumerable
                .Range(0, days)
                .Select(offset =>
                {
                    var date =
                        periodStartUtc.Date.AddDays(offset);

                    if (dailyLookup.TryGetValue(
                        date,
                        out var usage))
                    {
                        return new DeveloperUsageDailyVm
                        {
                            DateUtc = date,
                            Requests = usage.Requests,

                            SuccessfulRequests =
                                usage.SuccessfulRequests,

                            FailedRequests =
                                usage.FailedRequests,

                            AverageResponseTimeMs =
                                Math.Round(
                                    usage.AverageResponseTimeMs,
                                    2)
                        };
                    }

                    return new DeveloperUsageDailyVm
                    {
                        DateUtc = date
                    };
                })
                .ToList();

            var endpointUsage =
                await usageQuery
                    .GroupBy(log => log.Endpoint)
                    .Select(group =>
                        new DeveloperUsageEndpointVm
                        {
                            Endpoint = group.Key,
                            Requests = group.LongCount(),

                            SuccessfulRequests =
                                group.LongCount(
                                    log =>
                                        log.StatusCode >= 200 &&
                                        log.StatusCode < 400),

                            FailedRequests =
                                group.LongCount(
                                    log =>
                                        log.StatusCode >= 400),

                            AverageResponseTimeMs =
                                group.Average(
                                    log =>
                                        (double)log.ResponseTimeMs)
                        })
                    .OrderByDescending(
                        item => item.Requests)
                    .Take(15)
                    .ToListAsync(cancellationToken);

            var statusUsage =
                await usageQuery
                    .GroupBy(log => log.StatusCode)
                    .Select(group =>
                        new DeveloperUsageStatusVm
                        {
                            StatusCode = group.Key,
                            Requests = group.LongCount()
                        })
                    .OrderBy(item => item.StatusCode)
                    .ToListAsync(cancellationToken);

            /*
             * Request-log filters are independent from the chart period.
             * When no custom dates are supplied, the selected days period is used.
             */
            var effectiveFromUtc =
                fromDate?.Date ??
                periodStartUtc.Date;

            var effectiveToUtc =
                toDate?.Date.AddDays(1) ??
                periodEndUtc.Date.AddDays(1);

            if (effectiveToUtc <= effectiveFromUtc)
            {
                effectiveToUtc =
                    effectiveFromUtc.AddDays(1);
            }

            var requestLogsQuery =
                _db.ApiUsageLogs
                    .AsNoTracking()
                    .Where(log =>
                        log.ApiClientId == application.Id &&
                        log.CreatedAt >= effectiveFromUtc &&
                        log.CreatedAt < effectiveToUtc);

            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                requestLogsQuery =
                    requestLogsQuery.Where(
                        log =>
                            log.Endpoint.Contains(endpoint) ||
                            (
                                log.QueryString != null &&
                                log.QueryString.Contains(endpoint)
                            ));
            }

            if (method != "all")
            {
                requestLogsQuery =
                    requestLogsQuery.Where(
                        log => log.HttpMethod == method);
            }

            requestLogsQuery =
                requestStatus switch
                {
                    "success" =>
                        requestLogsQuery.Where(
                            log =>
                                log.StatusCode >= 200 &&
                                log.StatusCode < 300),

                    "redirect" =>
                        requestLogsQuery.Where(
                            log =>
                                log.StatusCode >= 300 &&
                                log.StatusCode < 400),

                    "client-error" =>
                        requestLogsQuery.Where(
                            log =>
                                log.StatusCode >= 400 &&
                                log.StatusCode < 500),

                    "server-error" =>
                        requestLogsQuery.Where(
                            log =>
                                log.StatusCode >= 500),

                    "error" =>
                        requestLogsQuery.Where(
                            log =>
                                log.StatusCode >= 400),

                    _ => requestLogsQuery
                };

            var totalFilteredRequests =
                await requestLogsQuery.CountAsync(
                    cancellationToken);

            var totalPages = Math.Max(
                1,
                (int)Math.Ceiling(
                    totalFilteredRequests /
                    (double)pageSize));

            page = Math.Min(
                page,
                totalPages);

            var recentRequests =
                await requestLogsQuery
                    .OrderByDescending(
                        log => log.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(log =>
                        new DeveloperUsageRequestVm
                        {
                            Id = log.Id,
                            CreatedAtUtc = log.CreatedAt,
                            HttpMethod = log.HttpMethod,
                            Endpoint = log.Endpoint,
                            QueryString = log.QueryString,
                            ApiVersion = log.ApiVersion,
                            StatusCode = log.StatusCode,
                            ResponseTimeMs =
                                log.ResponseTimeMs,
                            IpAddress = log.IpAddress,
                            UserAgent = log.UserAgent
                        })
                    .ToListAsync(cancellationToken);

            var successRate =
                totalRequests == 0
                    ? 0
                    : successfulRequests * 100d /
                      totalRequests;

            var model =
                new DeveloperApplicationUsageViewModel
                {
                    ApplicationId = application.Id,
                    ApplicationName = application.Name,
                    Status = application.LifecycleStatus,
                    MaskedApiKey = application.MaskedApiKey,

                    SelectedDays = days,
                    PeriodStartUtc = periodStartUtc,
                    PeriodEndUtc = periodEndUtc,

                    TotalRequests = totalRequests,

                    SuccessfulRequests =
                        successfulRequests,

                    FailedRequests =
                        failedRequests,

                    SuccessRate = Math.Round(
                        successRate,
                        2),

                    AverageResponseTimeMs = Math.Round(
                        averageResponseTimeMs,
                        2),

                    RequestsToday = requestsToday,

                    RequestsThisMonth =
                        requestsThisMonth,

                    MonthlyQuota =
                        application.MonthlyQuota,

                    RemainingQuota =
                        remainingQuota,

                    EndpointFilter =
                        endpoint ?? string.Empty,

                    MethodFilter =
                        method,

                    StatusFilter =
                        requestStatus,

                    FromDateUtc =
                        effectiveFromUtc,

                    ToDateUtc =
                        effectiveToUtc.AddDays(-1),

                    PageNumber =
                        page,

                    PageSize =
                        pageSize,

                    TotalFilteredRequests =
                        totalFilteredRequests,

                    DailyUsage =
                        dailyUsage,

                    EndpointUsage =
                        endpointUsage,

                    StatusUsage =
                        statusUsage,

                    RecentRequests =
                        recentRequests
                };

            return View(model);
        }


        [HttpGet("/Developer/Applications/{id:int}/Edit")]
        public async Task<IActionResult> EditApplication(
        int id,
        CancellationToken cancellationToken = default)
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Challenge();
                }

                var application = await _db.ApiClients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        client =>
                            client.Id == id &&
                            client.UserId == userId,
                        cancellationToken);

                if (application is null)
                {
                    return NotFound();
                }

                var model = new DeveloperApplicationEditViewModel
                {
                    Id = application.Id,
                    Name = application.Name,
                    Website = application.Website,
                    Notes = application.Notes
                };

                return View(model);
            }

        [HttpPost("/Developer/Applications/{id:int}/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditApplication(
        int id,
        DeveloperApplicationEditViewModel model,
        CancellationToken cancellationToken = default)
            {
                if (id != model.Id)
                {
                    return BadRequest();
                }

                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Challenge();
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var application = await _db.ApiClients
                    .FirstOrDefaultAsync(
                        client =>
                            client.Id == id &&
                            client.UserId == userId,
                        cancellationToken);

                if (application is null)
                {
                    return NotFound();
                }

                application.Name = model.Name.Trim();

                application.Website =
                    string.IsNullOrWhiteSpace(model.Website)
                        ? null
                        : model.Website.Trim();

                application.Notes =
                    string.IsNullOrWhiteSpace(model.Notes)
                        ? null
                        : model.Notes.Trim();

                await _db.SaveChangesAsync(cancellationToken);

                TempData["SuccessMessage"] =
                    "Application details were updated successfully.";

                return RedirectToAction(
                    nameof(ApplicationDetails),
                    new { id = application.Id });
            }

        [HttpPost("/Developer/Applications/{id:int}/Toggle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleApplicationStatus(
        int id,
        CancellationToken cancellationToken = default)
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Challenge();
                }

                var application = await _db.ApiClients
                    .FirstOrDefaultAsync(
                        client =>
                            client.Id == id &&
                            client.UserId == userId,
                        cancellationToken);

                if (application is null)
                {
                    return NotFound();
                }

                if (application.IsRevoked)
                {
                    TempData["ErrorMessage"] =
                        "A revoked application cannot be enabled.";

                    return RedirectToAction(
                        nameof(ApplicationDetails),
                        new { id });
                }

                if (application.IsExpired)
                {
                    TempData["ErrorMessage"] =
                        "An expired application cannot be enabled. Rotate its key first.";

                    return RedirectToAction(
                        nameof(ApplicationDetails),
                        new { id });
                }

                application.IsActive =
                    !application.IsActive;

                await _db.SaveChangesAsync(cancellationToken);

                TempData["SuccessMessage"] =
                    application.IsActive
                        ? "Application enabled successfully."
                        : "Application disabled successfully.";

                return RedirectToAction(
                    nameof(ApplicationDetails),
                    new { id });
            }

        [HttpPost("/Developer/Applications/{id:int}/Rotate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RotateApplicationKey(
        int id,
        CancellationToken cancellationToken = default)
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Challenge();
                }

                var application = await _db.ApiClients
                    .FirstOrDefaultAsync(
                        client =>
                            client.Id == id &&
                            client.UserId == userId,
                        cancellationToken);

                if (application is null)
                {
                    return NotFound();
                }

                var keyMaterial =
                    _apiKeyService.GenerateApiKeyMaterial();

                var utcNow = DateTime.UtcNow;

                application.ApiKeyHash =
                    keyMaterial.Hash;

                application.ApiKeyPrefix =
                    keyMaterial.Prefix;

                application.IsActive = true;
                application.RevokedAt = null;
                application.LastUsedAt = null;
                application.LastRotatedAt = utcNow;

                // Developer-created rotations receive a new 365-day expiry.
                application.ExpiresAt =
                    utcNow.AddDays(365);

                await _db.SaveChangesAsync(cancellationToken);

                var model = new ApiKeyCreatedViewModel
                {
                    Title = "API key rotated",
                    Message =
                        "Your previous key stopped working immediately. " +
                        "Copy and store this new key now because it will not be shown again.",
                    RawApiKey = keyMaterial.RawKey,
                    ClientName = application.Name,
                    ExpiresAt = application.ExpiresAt,
                    ReturnUrl = Url.Action(
                        nameof(ApplicationDetails),
                        "Developer",
                        new { id = application.Id })
                        ?? $"/Developer/Applications/{application.Id}"
                };

                return View(
                    "ApplicationKeyCreated",
                    model);
            }           

        [HttpPost("/Developer/Applications/{id:int}/Revoke")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeApplication(
        int id,
        CancellationToken cancellationToken = default)
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Challenge();
                }

                var application = await _db.ApiClients
                    .FirstOrDefaultAsync(
                        client =>
                            client.Id == id &&
                            client.UserId == userId,
                        cancellationToken);

                if (application is null)
                {
                    return NotFound();
                }

                if (!application.IsRevoked)
                {
                    application.IsActive = false;
                    application.RevokedAt = DateTime.UtcNow;

                    await _db.SaveChangesAsync(cancellationToken);
                }

                TempData["SuccessMessage"] =
                    "Application access was revoked successfully.";

                return RedirectToAction(
                    nameof(ApplicationDetails),
                    new { id });
            }

        [HttpPost("/Developer/Applications/{id:int}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteApplication(
        int id,
        string confirmationName,
        CancellationToken cancellationToken = default)
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Challenge();
                }

                var application = await _db.ApiClients
                    .FirstOrDefaultAsync(
                        client =>
                            client.Id == id &&
                            client.UserId == userId,
                        cancellationToken);

                if (application is null)
                {
                    return NotFound();
                }

                if (string.IsNullOrWhiteSpace(confirmationName) ||
                    !string.Equals(
                        confirmationName.Trim(),
                        application.Name,
                        StringComparison.Ordinal))
                {
                    TempData["ErrorMessage"] =
                        $"Permanent deletion was cancelled. Enter the exact application name: {application.Name}";

                    return RedirectToAction(
                        nameof(ApplicationDetails),
                        new { id });
                }

                await using var transaction =
                    await _db.Database.BeginTransactionAsync(
                        cancellationToken);

                try
                {
                    /*
                     * Delete dependent usage records first to avoid a foreign-key
                     * constraint failure when cascade delete is not configured.
                     */
                    await _db.ApiUsageLogs
                        .Where(log => log.ApiClientId == application.Id)
                        .ExecuteDeleteAsync(cancellationToken);

                    _db.ApiClients.Remove(application);

                    await _db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    TempData["SuccessMessage"] =
                        $"Application \"{application.Name}\" was permanently deleted.";

                return RedirectToAction("Applications","Developer");

                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }


        [HttpGet("/Developer/Applications/{id:int}/Usage/Export")]
        public async Task<IActionResult> ExportApplicationUsageCsv(
        int id,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Challenge();
                }

                days = days switch
                {
                    7 => 7,
                    30 => 30,
                    90 => 90,
                    _ => 30
                };

                var application = await _db.ApiClients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        client =>
                            client.Id == id &&
                            client.UserId == userId,
                        cancellationToken);

                if (application is null)
                {
                    return NotFound();
                }

                var utcNow = DateTime.UtcNow;
                var periodStartUtc =
                    utcNow.Date.AddDays(-(days - 1));

                var logs = await _db.ApiUsageLogs
                    .AsNoTracking()
                    .Where(log =>
                        log.ApiClientId == application.Id &&
                        log.CreatedAt >= periodStartUtc &&
                        log.CreatedAt <= utcNow)
                    .OrderByDescending(log => log.CreatedAt)
                    .Select(log => new
                    {
                        log.CreatedAt,
                        log.HttpMethod,
                        log.ApiVersion,
                        log.Endpoint,
                        log.QueryString,
                        log.StatusCode,
                        log.ResponseTimeMs,
                        log.IpAddress,
                        log.UserAgent
                    })
                    .ToListAsync(cancellationToken);

                var csv = new StringBuilder();

                csv.AppendLine(
                    "UTC Time,Application,HTTP Method,API Version,Endpoint," +
                    "Query String,Status Code,Response Time (ms),IP Address,User Agent");

                foreach (var log in logs)
                {
                    csv.Append(CsvCell(
                        log.CreatedAt.ToString(
                            "yyyy-MM-dd HH:mm:ss",
                            CultureInfo.InvariantCulture)));

                    csv.Append(',');
                    csv.Append(CsvCell(application.Name));

                    csv.Append(',');
                    csv.Append(CsvCell(log.HttpMethod));

                    csv.Append(',');
                    csv.Append(CsvCell(log.ApiVersion));

                    csv.Append(',');
                    csv.Append(CsvCell(log.Endpoint));

                    csv.Append(',');
                    csv.Append(CsvCell(log.QueryString));

                    csv.Append(',');
                    csv.Append(log.StatusCode.ToString(
                        CultureInfo.InvariantCulture));

                    csv.Append(',');
                    csv.Append(log.ResponseTimeMs.ToString(
                        CultureInfo.InvariantCulture));

                    csv.Append(',');
                    csv.Append(CsvCell(log.IpAddress));

                    csv.Append(',');
                    csv.Append(CsvCell(log.UserAgent));

                    csv.AppendLine();
                }

                /*
                 * UTF-8 BOM helps Microsoft Excel correctly detect UTF-8.
                 */
                var preamble = Encoding.UTF8.GetPreamble();

                var csvBytes = Encoding.UTF8.GetBytes(
                    csv.ToString());

                var fileBytes = new byte[
                    preamble.Length + csvBytes.Length];

                Buffer.BlockCopy(
                    preamble,
                    0,
                    fileBytes,
                    0,
                    preamble.Length);

                Buffer.BlockCopy(
                    csvBytes,
                    0,
                    fileBytes,
                    preamble.Length,
                    csvBytes.Length);

                var safeApplicationName =
                    string.Concat(
                        application.Name
                            .Where(character =>
                                char.IsLetterOrDigit(character) ||
                                character is '-' or '_'))
                    .Trim();

                if (string.IsNullOrWhiteSpace(safeApplicationName))
                {
                    safeApplicationName =
                        $"application-{application.Id}";
                }

                var fileName =
                    $"{safeApplicationName}-api-usage-" +
                    $"{periodStartUtc:yyyyMMdd}-" +
                    $"{utcNow:yyyyMMdd}.csv";

                return File(
                    fileBytes,
                    "text/csv; charset=utf-8",
                    fileName);
            }

        private static string CsvCell(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            /*
             * Prevent CSV formula injection when the file is opened in
             * spreadsheet software.
             */
            var sanitized = value;

            if (sanitized[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            {
                sanitized = $"'{sanitized}";
            }

            var escaped = sanitized.Replace(
                "\"",
                "\"\"",
                StringComparison.Ordinal);

            return $"\"{escaped}\"";
        }
    }
}