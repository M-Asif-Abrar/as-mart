using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using AsMart.Web.Models.ViewModels;
using AsMart.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        [HttpGet("/Developer/Applications")]
        public async Task<IActionResult> Applications(
        CancellationToken cancellationToken = default)
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Challenge();
                }

                var utcNow = DateTime.UtcNow;

                var monthStartUtc = new DateTime(
                    utcNow.Year,
                    utcNow.Month,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc);

                var nextMonthUtc = monthStartUtc.AddMonths(1);

                var clients = await _db.ApiClients
                    .AsNoTracking()
                    .Where(client => client.UserId == userId)
                    .OrderByDescending(client => client.CreatedAt)
                    .ToListAsync(cancellationToken);

                var clientIds = clients
                    .Select(client => client.Id)
                    .ToList();

                var monthlyUsage = clientIds.Count == 0
                    ? new Dictionary<int, long>()
                    : await _db.ApiUsageLogs
                        .AsNoTracking()
                        .Where(log =>
                            log.ApiClientId.HasValue &&
                            clientIds.Contains(log.ApiClientId.Value) &&
                            log.CreatedAt >= monthStartUtc &&
                            log.CreatedAt < nextMonthUtc)
                        .GroupBy(log => log.ApiClientId!.Value)
                        .Select(group => new
                        {
                            ApiClientId = group.Key,
                            Requests = group.LongCount()
                        })
                        .ToDictionaryAsync(
                            item => item.ApiClientId,
                            item => item.Requests,
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
                                    Math.Max(client.MonthlyQuota, 1),
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

                var model = new DeveloperApplicationsViewModel
                {
                    TotalApplications =
                        clients.Count,

                    ActiveApplications =
                        clients.Count(client =>
                            client.IsUsable),

                    DisabledApplications =
                        clients.Count(client =>
                            !client.IsActive &&
                            !client.IsRevoked &&
                            !client.IsExpired),

                    ExpiredApplications =
                        clients.Count(client =>
                            client.IsExpired),

                    RevokedApplications =
                        clients.Count(client =>
                            client.IsRevoked),

                    RequestsThisMonth =
                        applications.Sum(application =>
                            application.RequestsThisMonth),

                    Applications =
                        applications
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

                    return RedirectToAction(
                        nameof(Applications));
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }

        [HttpGet("/Developer/Applications/{id:int}/Usage")]
        public async Task<IActionResult> ApplicationUsage(
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
                var periodEndUtc = utcNow;
                var periodStartUtc = utcNow.Date.AddDays(-(days - 1));

                var monthStartUtc = new DateTime(
                    utcNow.Year,
                    utcNow.Month,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc);

                var nextMonthUtc = monthStartUtc.AddMonths(1);

                var usageQuery = _db.ApiUsageLogs
                    .AsNoTracking()
                    .Where(log =>
                        log.ApiClientId == application.Id &&
                        log.CreatedAt >= periodStartUtc &&
                        log.CreatedAt <= periodEndUtc);

                var totalRequests = await usageQuery
                    .LongCountAsync(cancellationToken);

                var successfulRequests = await usageQuery
                    .LongCountAsync(
                        log =>
                            log.StatusCode >= 200 &&
                            log.StatusCode < 400,
                        cancellationToken);

                var failedRequests = await usageQuery
                    .LongCountAsync(
                        log => log.StatusCode >= 400,
                        cancellationToken);

                var averageResponseTimeMs = totalRequests == 0
                    ? 0
                    : await usageQuery.AverageAsync(
                        log => (double)log.ResponseTimeMs,
                        cancellationToken);

                var requestsToday = await _db.ApiUsageLogs
                    .AsNoTracking()
                    .LongCountAsync(
                        log =>
                            log.ApiClientId == application.Id &&
                            log.CreatedAt >= utcNow.Date,
                        cancellationToken);

                var requestsThisMonth = await _db.ApiUsageLogs
                    .AsNoTracking()
                    .LongCountAsync(
                        log =>
                            log.ApiClientId == application.Id &&
                            log.CreatedAt >= monthStartUtc &&
                            log.CreatedAt < nextMonthUtc,
                        cancellationToken);

                var remainingQuota = application.MonthlyQuota <= 0
                    ? long.MaxValue
                    : Math.Max(
                        application.MonthlyQuota - requestsThisMonth,
                        0);

                var groupedDailyUsage = await usageQuery
                    .GroupBy(log => log.CreatedAt.Date)
                    .Select(group => new
                    {
                        DateUtc = group.Key,

                        Requests = group.LongCount(),

                        SuccessfulRequests = group.LongCount(
                            log =>
                                log.StatusCode >= 200 &&
                                log.StatusCode < 400),

                        FailedRequests = group.LongCount(
                            log => log.StatusCode >= 400),

                        AverageResponseTimeMs = group.Average(
                            log => (double)log.ResponseTimeMs)
                    })
                    .OrderBy(item => item.DateUtc)
                    .ToListAsync(cancellationToken);

                var dailyLookup = groupedDailyUsage
                    .ToDictionary(
                        item => item.DateUtc.Date);

                var dailyUsage = Enumerable
                    .Range(0, days)
                    .Select(offset =>
                    {
                        var date = periodStartUtc.Date.AddDays(offset);

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

                                AverageResponseTimeMs = Math.Round(
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

                var endpointUsage = await usageQuery
                    .GroupBy(log => log.Endpoint)
                    .Select(group => new DeveloperUsageEndpointVm
                    {
                        Endpoint = group.Key,

                        Requests = group.LongCount(),

                        SuccessfulRequests = group.LongCount(
                            log =>
                                log.StatusCode >= 200 &&
                                log.StatusCode < 400),

                        FailedRequests = group.LongCount(
                            log => log.StatusCode >= 400),

                        AverageResponseTimeMs = group.Average(
                            log => (double)log.ResponseTimeMs)
                    })
                    .OrderByDescending(item => item.Requests)
                    .Take(15)
                    .ToListAsync(cancellationToken);

                var statusUsage = await usageQuery
                    .GroupBy(log => log.StatusCode)
                    .Select(group => new DeveloperUsageStatusVm
                    {
                        StatusCode = group.Key,
                        Requests = group.LongCount()
                    })
                    .OrderBy(item => item.StatusCode)
                    .ToListAsync(cancellationToken);

                var recentRequests = await _db.ApiUsageLogs
                    .AsNoTracking()
                    .Where(log =>
                        log.ApiClientId == application.Id)
                    .OrderByDescending(log => log.CreatedAt)
                    .Take(100)
                    .Select(log => new DeveloperUsageRequestVm
                    {
                        Id = log.Id,
                        CreatedAtUtc = log.CreatedAt,
                        HttpMethod = log.HttpMethod,
                        Endpoint = log.Endpoint,
                        QueryString = log.QueryString,
                        ApiVersion = log.ApiVersion,
                        StatusCode = log.StatusCode,
                        ResponseTimeMs = log.ResponseTimeMs,
                        IpAddress = log.IpAddress,
                        UserAgent = log.UserAgent
                    })
                    .ToListAsync(cancellationToken);

                var successRate = totalRequests == 0
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
                        SuccessfulRequests = successfulRequests,
                        FailedRequests = failedRequests,

                        SuccessRate = Math.Round(
                            successRate,
                            2),

                        AverageResponseTimeMs = Math.Round(
                            averageResponseTimeMs,
                            2),

                        RequestsToday = requestsToday,
                        RequestsThisMonth = requestsThisMonth,

                        MonthlyQuota =
                            application.MonthlyQuota,

                        RemainingQuota =
                            remainingQuota,

                        DailyUsage = dailyUsage,
                        EndpointUsage = endpointUsage,
                        StatusUsage = statusUsage,
                        RecentRequests = recentRequests
                    };

                return View(model);
            }
    }
}