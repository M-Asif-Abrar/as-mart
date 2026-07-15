using AsMart.Web.Data;
using AsMart.Web.Models.ViewModels;
using AsMart.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public sealed class ApiKeysController : Controller
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

        [HttpGet]
        public async Task<IActionResult> Index(
            string? q,
            CancellationToken cancellationToken)
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
                    (x.ApiKeyPrefix != null &&
                     x.ApiKeyPrefix.Contains(q)) ||
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
                .ToListAsync(cancellationToken);

            ViewBag.Search = q;

            /*
             * Do not expose a raw-key masking delegate.
             * The view must use client.MaskedApiKey.
             */
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRateLimit(
            int id,
            int rateLimitPerMinute,
            CancellationToken cancellationToken)
        {
            rateLimitPerMinute =
                Math.Clamp(rateLimitPerMinute, 1, 10_000);

            var client = await _db.ApiClients
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (client is null)
            {
                return NotFound();
            }

            client.RateLimitPerMinute =
                rateLimitPerMinute;

            await _db.SaveChangesAsync(
                cancellationToken);

            TempData["Success"] =
                "Rate limit updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMonthlyQuota(
            int id,
            int monthlyQuota,
            CancellationToken cancellationToken)
        {
            monthlyQuota =
                Math.Clamp(monthlyQuota, 0, 10_000_000);

            var client = await _db.ApiClients
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (client is null)
            {
                return NotFound();
            }

            client.MonthlyQuota = monthlyQuota;

            await _db.SaveChangesAsync(
                cancellationToken);

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
            string? notes,
            CancellationToken cancellationToken)
        {
            var client = await _db.ApiClients
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (client is null)
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
             * The selected date remains valid until the end
             * of the selected UTC calendar day.
             */
            var expirationUtc = DateTime.SpecifyKind(
                expiresOn.Value.Date
                    .AddDays(1)
                    .AddTicks(-1),
                DateTimeKind.Utc);

            client.ExpiresAt = expirationUtc;
            client.Notes = notes;

            if (expirationUtc <= DateTime.UtcNow)
            {
                client.IsActive = false;
            }

            await _db.SaveChangesAsync(
                cancellationToken);

            TempData["Success"] =
                "API key lifecycle settings updated.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(
            int id,
            CancellationToken cancellationToken)
        {
            var client = await _db.ApiClients
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (client is null)
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

            await _db.SaveChangesAsync(
                cancellationToken);

            TempData["Success"] =
                client.IsActive
                    ? "API key enabled successfully."
                    : "API key disabled successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(
            int id,
            CancellationToken cancellationToken)
        {
            var client = await _db.ApiClients
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (client is null)
            {
                return NotFound();
            }

            if (!client.RevokedAt.HasValue)
            {
                client.IsActive = false;
                client.RevokedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(
                    cancellationToken);
            }

            TempData["Success"] =
                "API key permanently revoked.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rotate(
            int id,
            int expirationDays = 365,
            CancellationToken cancellationToken = default)
        {
            if (!AllowedExpirationDays.Contains(
                    expirationDays))
            {
                expirationDays = 365;
            }

            var client = await _db.ApiClients
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (client is null)
            {
                return NotFound();
            }

            var utcNow = DateTime.UtcNow;
            var material =
                _apiKeyService.GenerateApiKeyMaterial();

            /*
             * Phase 2 security:
             * only the hash and non-sensitive prefix are persisted.
             */
            client.ApiKeyHash = material.Hash;
            client.ApiKeyPrefix = material.Prefix;

            client.IsActive = true;
            client.RevokedAt = null;
            client.LastRotatedAt = utcNow;
            client.LastUsedAt = null;
            client.ExpiresAt =
                utcNow.AddDays(expirationDays);

            await _db.SaveChangesAsync(
                cancellationToken);

            /*
             * The full raw key is displayed once in this response.
             * It is not stored in TempData, session, logs, or SQL Server.
             */
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
                        Url.Action(
                            nameof(Index),
                            "ApiKeys",
                            new { area = "Admin" })
                        ?? "/Admin/ApiKeys"
                });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            var client = await _db.ApiClients
                .AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (client is null)
            {
                return NotFound();
            }

            return View(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id,
            CancellationToken cancellationToken)
        {
            var client = await _db.ApiClients
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (client is null)
            {
                return NotFound();
            }

            _db.ApiClients.Remove(client);

            await _db.SaveChangesAsync(
                cancellationToken);

            TempData["Success"] =
                "API key deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}