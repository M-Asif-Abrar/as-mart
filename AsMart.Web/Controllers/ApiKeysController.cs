using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using AsMart.Web.Models.ViewModels;
using AsMart.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Controllers
{
    [Authorize]
    public sealed class ApiKeysController : Controller
    {
        private static readonly int[] AllowedExpirationDays =
        {
            30,
            90,
            180,
            365
        };

        private const int MaximumKeysPerUser = 10;
        private const int DefaultRateLimitPerMinute = 60;
        private const int DefaultMonthlyQuota = 10_000;

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApiKeyService _apiKeyService;

        public ApiKeysController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IApiKeyService apiKeyService)
        {
            _db = db;
            _userManager = userManager;
            _apiKeyService = apiKeyService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (userId is null)
            {
                return Challenge();
            }

            var clients = await _db.ApiClients
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            return View(clients);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string name,
            string? website,
            int expirationDays = 365,
            string? notes = null,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            if (userId is null)
            {
                return Challenge();
            }

            name = name?.Trim() ?? string.Empty;
            website = NormalizeOptionalValue(website);
            notes = NormalizeOptionalValue(notes);

            var validationError = ValidateInput(name, website, notes);

            if (validationError is not null)
            {
                TempData["Error"] = validationError;
                return RedirectToAction(nameof(Index));
            }

            expirationDays = NormalizeExpirationDays(expirationDays);

            var clientCount = await _db.ApiClients
                .CountAsync(
                    x => x.UserId == userId,
                    cancellationToken);

            if (clientCount >= MaximumKeysPerUser)
            {
                TempData["Error"] =
                    $"You can create a maximum of {MaximumKeysPerUser} API keys.";

                return RedirectToAction(nameof(Index));
            }

            var utcNow = DateTime.UtcNow;
            var material = _apiKeyService.GenerateApiKeyMaterial();

            var client = new ApiClient
            {
                Name = name,
                Website = website,
                Notes = notes,

                // Never persist the full raw key.
                ApiKeyHash = material.Hash,
                ApiKeyPrefix = material.Prefix,

                UserId = userId,
                IsActive = true,
                RateLimitPerMinute = DefaultRateLimitPerMinute,
                MonthlyQuota = DefaultMonthlyQuota,
                CreatedAt = utcNow,
                ExpiresAt = utcNow.AddDays(expirationDays),
                RevokedAt = null,
                LastRotatedAt = null,
                LastUsedAt = null
            };

            _db.ApiClients.Add(client);
            await _db.SaveChangesAsync(cancellationToken);

            return View(
                "~/Views/Shared/ApiKeys/KeyCreated.cshtml",
                new ApiKeyCreatedViewModel
                {
                    Title = "API key created",
                    Message =
                        "Copy this API key now. It will never be shown again.",
                    RawApiKey = material.RawKey,
                    ClientName = client.Name,
                    ExpiresAt = client.ExpiresAt,
                    ReturnUrl =
                        Url.Action(nameof(Index)) ?? "/ApiKeys"
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rotate(
            int id,
            int expirationDays = 365,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            if (userId is null)
            {
                return Challenge();
            }

            expirationDays = NormalizeExpirationDays(expirationDays);

            var client = await _db.ApiClients
                .FirstOrDefaultAsync(
                    x => x.Id == id && x.UserId == userId,
                    cancellationToken);

            if (client is null)
            {
                return NotFound();
            }

            var utcNow = DateTime.UtcNow;
            var material = _apiKeyService.GenerateApiKeyMaterial();

            client.ApiKeyHash = material.Hash;
            client.ApiKeyPrefix = material.Prefix;
            client.IsActive = true;
            client.RevokedAt = null;
            client.LastRotatedAt = utcNow;
            client.LastUsedAt = null;
            client.ExpiresAt = utcNow.AddDays(expirationDays);

            await _db.SaveChangesAsync(cancellationToken);

            return View(
                "~/Views/Shared/ApiKeys/KeyCreated.cshtml",
                new ApiKeyCreatedViewModel
                {
                    Title = "API key rotated",
                    Message =
                        "Copy the new API key now. The previous key is no longer valid.",
                    RawApiKey = material.RawKey,
                    ClientName = client.Name,
                    ExpiresAt = client.ExpiresAt,
                    ReturnUrl =
                        Url.Action(nameof(Index)) ?? "/ApiKeys"
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable(
            int id,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (userId is null)
            {
                return Challenge();
            }

            var client = await _db.ApiClients
                .FirstOrDefaultAsync(
                    x => x.Id == id && x.UserId == userId,
                    cancellationToken);

            if (client is null)
            {
                return NotFound();
            }

            if (client.RevokedAt.HasValue)
            {
                TempData["Error"] =
                    "A revoked API key cannot be disabled or reactivated. Rotate it instead.";

                return RedirectToAction(nameof(Index));
            }

            client.IsActive = false;
            await _db.SaveChangesAsync(cancellationToken);

            TempData["Success"] = "API key disabled successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(
            int id,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (userId is null)
            {
                return Challenge();
            }

            var client = await _db.ApiClients
                .FirstOrDefaultAsync(
                    x => x.Id == id && x.UserId == userId,
                    cancellationToken);

            if (client is null)
            {
                return NotFound();
            }

            if (!client.RevokedAt.HasValue)
            {
                client.IsActive = false;
                client.RevokedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }

            TempData["Success"] =
                "API key permanently revoked. Rotate it to generate a new credential.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(
            int id,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (userId is null)
            {
                return Challenge();
            }

            var client = await _db.ApiClients
                .FirstOrDefaultAsync(
                    x => x.Id == id && x.UserId == userId,
                    cancellationToken);

            if (client is null)
            {
                return NotFound();
            }

            if (client.RevokedAt.HasValue)
            {
                TempData["Error"] =
                    "A revoked API key cannot be activated. Rotate it instead.";

                return RedirectToAction(nameof(Index));
            }

            if (client.ExpiresAt.HasValue &&
                client.ExpiresAt.Value <= DateTime.UtcNow)
            {
                TempData["Error"] =
                    "An expired API key cannot be activated. Rotate it instead.";

                return RedirectToAction(nameof(Index));
            }

            client.IsActive = true;
            await _db.SaveChangesAsync(cancellationToken);

            TempData["Success"] = "API key activated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Usage(
            int id,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (userId is null)
            {
                return Challenge();
            }

            var client = await _db.ApiClients
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == id && x.UserId == userId,
                    cancellationToken);

            if (client is null)
            {
                return NotFound();
            }

            var now = DateTime.UtcNow;
            var today = now.Date;

            var monthStart = new DateTime(
                now.Year,
                now.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

            var nextMonth = monthStart.AddMonths(1);

            var monthlyLogs = _db.ApiUsageLogs
                .AsNoTracking()
                .Where(x =>
                    x.ApiClientId == client.Id &&
                    x.CreatedAt >= monthStart &&
                    x.CreatedAt < nextMonth);

            var requestsThisMonth =
                await monthlyLogs.CountAsync(cancellationToken);

            var failedRequests =
                await monthlyLogs.CountAsync(
                    x => x.StatusCode >= 400,
                    cancellationToken);

            var successfulRequests =
                await monthlyLogs.CountAsync(
                    x => x.StatusCode >= 200 && x.StatusCode < 400,
                    cancellationToken);

            var averageResponseTime =
                requestsThisMonth > 0
                    ? await monthlyLogs.AverageAsync(
                        x => x.ResponseTimeMs,
                        cancellationToken)
                    : 0;

            var requestsToday =
                await _db.ApiUsageLogs
                    .AsNoTracking()
                    .CountAsync(
                        x => x.ApiClientId == client.Id && x.CreatedAt >= today,
                        cancellationToken);

            var topEndpoints =
                await monthlyLogs
                    .GroupBy(x => x.Endpoint)
                    .Select(group => new MyApiEndpointUsageVm
                    {
                        Endpoint = group.Key,
                        RequestCount = group.Count()
                    })
                    .OrderByDescending(x => x.RequestCount)
                    .Take(10)
                    .ToListAsync(cancellationToken);

            var recentRequests =
                await _db.ApiUsageLogs
                    .AsNoTracking()
                    .Where(x => x.ApiClientId == client.Id)
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(50)
                    .Select(x => new MyApiRecentRequestVm
                    {
                        CreatedAt = x.CreatedAt,
                        HttpMethod = x.HttpMethod,
                        Endpoint = x.Endpoint,
                        QueryString = x.QueryString,
                        StatusCode = x.StatusCode,
                        ResponseTimeMs = x.ResponseTimeMs
                    })
                    .ToListAsync(cancellationToken);

            var model = new MyApiUsageViewModel
            {
                ApiClientId = client.Id,
                ClientName = client.Name,
                MaskedApiKey = client.MaskedApiKey,
                Website = client.Website,
                IsActive = client.IsUsable,
                RateLimitPerMinute = client.RateLimitPerMinute,
                MonthlyQuota = client.MonthlyQuota,
                RequestsThisMonth = requestsThisMonth,
                RemainingQuota = client.MonthlyQuota <= 0
                    ? int.MaxValue
                    : Math.Max(client.MonthlyQuota - requestsThisMonth, 0),
                RequestsToday = requestsToday,
                FailedRequestsThisMonth = failedRequests,
                SuccessRate = requestsThisMonth > 0
                    ? successfulRequests * 100d / requestsThisMonth
                    : 0,
                AverageResponseTimeMs = averageResponseTime,
                CreatedAt = client.CreatedAt,
                LastUsedAt = client.LastUsedAt,
                QuotaResetAt = nextMonth,
                TopEndpoints = topEndpoints,
                RecentRequests = recentRequests
            };

            return View(model);
        }

        private string? GetCurrentUserId()
        {
            var userId = _userManager.GetUserId(User);

            return string.IsNullOrWhiteSpace(userId)
                ? null
                : userId;
        }

        private static int NormalizeExpirationDays(int expirationDays)
        {
            return AllowedExpirationDays.Contains(expirationDays)
                ? expirationDays
                : 365;
        }

        private static string? ValidateInput(
            string name,
            string? website,
            string? notes)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "API key name is required.";
            }

            if (name.Length > 150)
            {
                return "API key name cannot exceed 150 characters.";
            }

            if (website?.Length > 300)
            {
                return "Website URL cannot exceed 300 characters.";
            }

            if (notes?.Length > 1000)
            {
                return "Notes cannot exceed 1,000 characters.";
            }

            return null;
        }

        private static string? NormalizeOptionalValue(string? value)
        {
            value = value?.Trim();

            return string.IsNullOrWhiteSpace(value)
                ? null
                : value;
        }
    }
}