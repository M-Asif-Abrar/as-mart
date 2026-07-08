using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
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

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var keys = await _db.ApiClients
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            ViewBag.MaskApiKey = new Func<string, string>(_apiKeyService.MaskApiKey);

            return View(keys);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, string? website)
        {
            var userId = _userManager.GetUserId(User);

            var activeKeys = await _db.ApiClients
                .CountAsync(x => x.UserId == userId && x.IsActive);

            if (activeKeys >= 3)
            {
                TempData["Error"] = "You can create maximum 3 active API keys.";
                return RedirectToAction(nameof(Index));
            }

            var apiKey = _apiKeyService.GenerateApiKey();

            var client = new ApiClient
            {
                Name = string.IsNullOrWhiteSpace(name) ? "Default API Key" : name.Trim(),
                Website = website?.Trim(),
                ApiKey = apiKey,
                UserId = userId,
                IsActive = true,
                RateLimitPerMinute = 60,
                CreatedAt = DateTime.UtcNow
            };

            _db.ApiClients.Add(client);
            await _db.SaveChangesAsync();

            TempData["NewApiKey"] = apiKey;
            TempData["Success"] = "API key created successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(int id)
        {
            var userId = _userManager.GetUserId(User);

            var key = await _db.ApiClients
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (key == null)
                return NotFound();

            key.IsActive = false;
            await _db.SaveChangesAsync();

            TempData["Success"] = "API key revoked successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Regenerate(int id)
        {
            var userId = _userManager.GetUserId(User);

            var key = await _db.ApiClients
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (key == null)
                return NotFound();

            var newApiKey = _apiKeyService.GenerateApiKey();

            key.ApiKey = newApiKey;
            key.IsActive = true;
            key.CreatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["NewApiKey"] = newApiKey;
            TempData["Success"] = "API key regenerated successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}