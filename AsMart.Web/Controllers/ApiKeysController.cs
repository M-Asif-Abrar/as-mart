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
    public class ApiKeysController : Controller
    {
        private static readonly int[] AllowedExpirationDays =
        {
            30,
            90,
            180,
            365
        };

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
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var clients = await _db.ApiClients
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            ViewBag.MaskApiKey =
                new Func<string, string>(_apiKeyService.MaskApiKey);

            return View(clients);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string name,
            string? website,
            int expirationDays = 365,
            string? notes = null)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            name = name?.Trim() ?? string.Empty;
            website = NormalizeOptionalValue(website);
            notes = NormalizeOptionalValue(notes);

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "API key name is required.";
                return RedirectToAction(nameof(Index));
            }

            if (name.Length > 150)
            {
                TempData["Error"] =
                    "API key name cannot exceed 150 characters.";

                return RedirectToAction(nameof(Index));
            }

            if (website?.Length > 300)
            {
                TempData["Error"] =
                    "Website URL cannot exceed 300 characters.";

                return RedirectToAction(nameof(Index));
            }

            if (notes?.Length > 1000)
            {
                TempData["Error"] =
                    "Notes cannot exceed 1,000 characters.";

                return RedirectToAction(nameof(Index));
            }

            if (!AllowedExpirationDays.Contains(expirationDays))
            {
                expirationDays = 365;
            }

            var clientCount = await _db.ApiClients
                .CountAsync(x => x.UserId == userId);

            if (clientCount >= 10)
            {
                TempData["Error"] =
                    "You can create a maximum of 10 API keys.";

                return RedirectToAction(nameof(Index));
            }

            var utcNow = DateTime.UtcNow;
            var apiKey = _apiKeyService.GenerateApiKey();

            var client = new ApiClient
            {
                Name = name,
                Website = website,
                Notes = notes,
                ApiKey = apiKey,
                UserId = userId,
                IsActive = true,
                RateLimitPerMinute = 60,
                MonthlyQuota = 10_000,
                CreatedAt = utcNow,
                ExpiresAt = utcNow.AddDays(expirationDays),
                RevokedAt = null,
                LastRotatedAt = null
            };

            _db.ApiClients.Add(client);
            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"API key created successfully. It expires in {expirationDays} days.";

            /*
             * The plain key is shown immediately after creation.
             */
            TempData["NewApiKey"] = apiKey;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rotate(
            int id,
            int expirationDays = 365)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            if (!AllowedExpirationDays.Contains(expirationDays))
            {
                expirationDays = 365;
            }

            var client = await _db.ApiClients
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserId == userId);

            if (client == null)
            {
                return NotFound();
            }

            var utcNow = DateTime.UtcNow;
            var newApiKey = _apiKeyService.GenerateApiKey();

            /*
             * Replacing ApiKey immediately invalidates the old credential.
             */
            client.ApiKey = newApiKey;
            client.IsActive = true;
            client.RevokedAt = null;
            client.LastRotatedAt = utcNow;
            client.LastUsedAt = null;
            client.ExpiresAt = utcNow.AddDays(expirationDays);

            await _db.SaveChangesAsync();

            TempData["Success"] =
                "API key rotated successfully. The previous key is no longer valid.";

            TempData["NewApiKey"] = newApiKey;

            return RedirectToAction(nameof(Index));
        }

        /*
         * Temporary disabling.
         * This does not mark the credential as permanently revoked.
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable(int id)
        {
            var userId = _userManager.GetUserId(User);

            var client = await _db.ApiClients
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserId == userId);

            if (client == null)
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

            await _db.SaveChangesAsync();

            TempData["Success"] =
                "API key disabled successfully.";

            return RedirectToAction(nameof(Index));
        }

        /*
         * Permanent revocation.
         * A revoked key cannot be activated again.
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(int id)
        {
            var userId = _userManager.GetUserId(User);

            var client = await _db.ApiClients
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserId == userId);

            if (client == null)
            {
                return NotFound();
            }

            if (!client.RevokedAt.HasValue)
            {
                client.IsActive = false;
                client.RevokedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
            }

            TempData["Success"] =
                "API key permanently revoked. Rotate it to generate a new credential.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var userId = _userManager.GetUserId(User);

            var client = await _db.ApiClients
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserId == userId);

            if (client == null)
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

            await _db.SaveChangesAsync();

            TempData["Success"] =
                "API key activated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Usage(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var client = await _db.ApiClients
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserId == userId);

            if (client == null)
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
                await monthlyLogs.CountAsync();

            var failedRequests =
                await monthlyLogs.CountAsync(x =>
                    x.StatusCode >= 400);

            var successfulRequests =
                await monthlyLogs.CountAsync(x =>
                    x.StatusCode >= 200 &&
                    x.StatusCode < 400);

            var averageResponseTime =
                await monthlyLogs.AnyAsync()
                    ? await monthlyLogs.AverageAsync(
                        x => x.ResponseTimeMs)
                    : 0;

            var model = new MyApiUsageViewModel
            {
                ApiClientId = client.Id,
                ClientName = client.Name,
                MaskedApiKey =
                    _apiKeyService.MaskApiKey(client.ApiKey),
                Website = client.Website,
                IsActive = client.IsUsable,
                RateLimitPerMinute =
                    client.RateLimitPerMinute,
                MonthlyQuota = client.MonthlyQuota,
                RequestsThisMonth = requestsThisMonth,

                RemainingQuota = client.MonthlyQuota <= 0
                    ? int.MaxValue
                    : Math.Max(
                        client.MonthlyQuota - requestsThisMonth,
                        0),

                RequestsToday = await _db.ApiUsageLogs
                    .AsNoTracking()
                    .CountAsync(x =>
                        x.ApiClientId == client.Id &&
                        x.CreatedAt >= today),

                FailedRequestsThisMonth = failedRequests,

                SuccessRate = requestsThisMonth > 0
                    ? successfulRequests * 100d /
                      requestsThisMonth
                    : 0,

                AverageResponseTimeMs = averageResponseTime,
                CreatedAt = client.CreatedAt,
                LastUsedAt = client.LastUsedAt,
                QuotaResetAt = nextMonth,

                TopEndpoints = await monthlyLogs
                    .GroupBy(x => x.Endpoint)
                    .Select(group =>
                        new MyApiEndpointUsageVm
                        {
                            Endpoint = group.Key,
                            RequestCount = group.Count()
                        })
                    .OrderByDescending(x => x.RequestCount)
                    .Take(10)
                    .ToListAsync(),

                RecentRequests = await _db.ApiUsageLogs
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
                    .ToListAsync()
            };

            return View(model);
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