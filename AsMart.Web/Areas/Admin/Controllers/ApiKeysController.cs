using AsMart.Web.Data;
using AsMart.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
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
        private readonly IApiKeyService _apiKeyService;

        public ApiKeysController(
            ApplicationDbContext db,
            IApiKeyService apiKeyService)
        {
            _db = db;
            _apiKeyService = apiKeyService;
        }

        public async Task<IActionResult> Index(string? q)
        {
            var query = _db.ApiClients
                .AsNoTracking()
                .Include(x => x.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();

                query = query.Where(x =>
                    x.Name.Contains(q) ||
                    x.ApiKey.Contains(q) ||
                    (x.Website != null &&
                     x.Website.Contains(q)) ||
                    (x.Notes != null &&
                     x.Notes.Contains(q)) ||
                    (x.User != null &&
                     x.User.Email != null &&
                     x.User.Email.Contains(q)));
            }

            var model = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            ViewBag.Search = q;
            ViewBag.MaskApiKey =
                new Func<string, string>(_apiKeyService.MaskApiKey);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRateLimit(
            int id,
            int rateLimitPerMinute)
        {
            rateLimitPerMinute =
                Math.Clamp(rateLimitPerMinute, 1, 10_000);

            var client = await _db.ApiClients
                .FirstOrDefaultAsync(x => x.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            client.RateLimitPerMinute = rateLimitPerMinute;

            await _db.SaveChangesAsync();

            TempData["Success"] =
                "Rate limit updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMonthlyQuota(
            int id,
            int monthlyQuota)
        {
            monthlyQuota =
                Math.Clamp(monthlyQuota, 0, 10_000_000);

            var client = await _db.ApiClients
                .FirstOrDefaultAsync(x => x.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            client.MonthlyQuota = monthlyQuota;

            await _db.SaveChangesAsync();

            TempData["Success"] =
                monthlyQuota == 0
                    ? "Monthly quota updated to unlimited."
                    : $"Monthly quota updated to {monthlyQuota:N0} requests.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLifecycle(
            int id,
            DateTime? expiresOn,
            string? notes)
        {
            var client = await _db.ApiClients
                .FirstOrDefaultAsync(x => x.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            notes = string.IsNullOrWhiteSpace(notes)
                ? null
                : notes.Trim();

            if (notes?.Length > 1000)
            {
                TempData["Error"] =
                    "Notes cannot exceed 1,000 characters.";

                return RedirectToAction(nameof(Index));
            }

            if (!expiresOn.HasValue)
            {
                TempData["Error"] =
                    "An expiration date is required.";

                return RedirectToAction(nameof(Index));
            }

            /*
             * The selected expiration date remains valid until the end
             * of that UTC calendar day.
             */
            var expirationUtc = DateTime.SpecifyKind(
                expiresOn.Value.Date
                    .AddDays(1)
                    .AddTicks(-1),
                DateTimeKind.Utc);

            client.ExpiresAt = expirationUtc;
            client.Notes = notes;

            /*
             * If the administrator sets a past date, immediately make
             * the key inactive.
             */
            if (expirationUtc <= DateTime.UtcNow)
            {
                client.IsActive = false;
            }

            await _db.SaveChangesAsync();

            TempData["Success"] =
                "API key lifecycle settings updated.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var client = await _db.ApiClients
                .FirstOrDefaultAsync(x => x.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            if (!client.IsActive)
            {
                if (client.RevokedAt.HasValue)
                {
                    TempData["Error"] =
                        "A revoked key cannot be enabled. Rotate it instead.";

                    return RedirectToAction(nameof(Index));
                }

                if (client.ExpiresAt.HasValue &&
                    client.ExpiresAt.Value <= DateTime.UtcNow)
                {
                    TempData["Error"] =
                        "An expired key cannot be enabled. Extend its expiration date or rotate it.";

                    return RedirectToAction(nameof(Index));
                }
            }

            client.IsActive = !client.IsActive;

            await _db.SaveChangesAsync();

            TempData["Success"] =
                client.IsActive
                    ? "API key enabled successfully."
                    : "API key disabled successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(int id)
        {
            var client = await _db.ApiClients
                .FirstOrDefaultAsync(x => x.Id == id);

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
                "API key permanently revoked.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rotate(
            int id,
            int expirationDays = 365)
        {
            if (!AllowedExpirationDays.Contains(expirationDays))
            {
                expirationDays = 365;
            }

            var client = await _db.ApiClients
                .FirstOrDefaultAsync(x => x.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            var utcNow = DateTime.UtcNow;
            var newApiKey = _apiKeyService.GenerateApiKey();

            client.ApiKey = newApiKey;
            client.IsActive = true;
            client.RevokedAt = null;
            client.LastRotatedAt = utcNow;
            client.LastUsedAt = null;
            client.ExpiresAt =
                utcNow.AddDays(expirationDays);

            await _db.SaveChangesAsync();

            TempData["Success"] =
                "API key rotated successfully. The previous key is no longer valid.";

            TempData["NewApiKey"] = newApiKey;

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var client = await _db.ApiClients
                .AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = await _db.ApiClients
                .FirstOrDefaultAsync(x => x.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            _db.ApiClients.Remove(client);

            await _db.SaveChangesAsync();

            TempData["Success"] =
                "API key deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}