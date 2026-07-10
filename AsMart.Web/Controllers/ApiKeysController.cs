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
            string? website)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            name = name?.Trim() ?? string.Empty;
            website = website?.Trim();

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

            if (!string.IsNullOrWhiteSpace(website) &&
                website.Length > 300)
            {
                TempData["Error"] =
                    "Website URL cannot exceed 300 characters.";

                return RedirectToAction(nameof(Index));
            }

            var clientCount = await _db.ApiClients
                .CountAsync(x => x.UserId == userId);

            if (clientCount >= 10)
            {
                TempData["Error"] =
                    "You can create a maximum of 10 API keys.";

                return RedirectToAction(nameof(Index));
            }

            var apiKey = _apiKeyService.GenerateApiKey();

            var client = new ApiClient
            {
                Name = name,
                Website = string.IsNullOrWhiteSpace(website)
                    ? null
                    : website,
                ApiKey = apiKey,
                UserId = userId,
                IsActive = true,
                RateLimitPerMinute = 60,
                MonthlyQuota = 10000,
                CreatedAt = DateTime.UtcNow
            };

            _db.ApiClients.Add(client);
            await _db.SaveChangesAsync();

            TempData["Success"] =
                "API key created successfully.";

            TempData["NewApiKey"] = apiKey;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Regenerate(int id)
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

            var newApiKey = _apiKeyService.GenerateApiKey();

            client.ApiKey = newApiKey;
            client.IsActive = true;
            client.LastUsedAt = null;

            await _db.SaveChangesAsync();

            TempData["Success"] =
                "API key regenerated. The previous key is no longer valid.";

            TempData["NewApiKey"] = newApiKey;

            return RedirectToAction(nameof(Index));
        }

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

            client.IsActive = false;
            await _db.SaveChangesAsync();

            TempData["Success"] =
                "API key revoked successfully.";

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
                    ? await monthlyLogs
                        .AverageAsync(x => x.ResponseTimeMs)
                    : 0;

            var model = new MyApiUsageViewModel
            {
                ApiClientId = client.Id,
                ClientName = client.Name,
                MaskedApiKey =
                    _apiKeyService.MaskApiKey(client.ApiKey),
                Website = client.Website,
                IsActive = client.IsActive,
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
    }
}