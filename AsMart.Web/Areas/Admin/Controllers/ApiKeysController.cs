using AsMart.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ApiKeysController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ApiKeysController(ApplicationDbContext db)
        {
            _db = db;
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
                    (x.Website != null && x.Website.Contains(q)) ||
                    (x.User != null && x.User.Email != null && x.User.Email.Contains(q)));
            }

            var model = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            ViewBag.Search = q;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRateLimit(int id, int rateLimitPerMinute)
        {
            if (rateLimitPerMinute < 1)
                rateLimitPerMinute = 60;

            if (rateLimitPerMinute > 10000)
                rateLimitPerMinute = 10000;

            var client = await _db.ApiClients.FirstOrDefaultAsync(x => x.Id == id);

            if (client == null)
                return NotFound();

            client.RateLimitPerMinute = rateLimitPerMinute;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Rate limit updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var client = await _db.ApiClients.FirstOrDefaultAsync(x => x.Id == id);

            if (client == null)
                return NotFound();

            client.IsActive = !client.IsActive;
            await _db.SaveChangesAsync();

            TempData["Success"] = "API key status updated.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var client = await _db.ApiClients
                .AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (client == null)
                return NotFound();

            return View(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = await _db.ApiClients.FirstOrDefaultAsync(x => x.Id == id);

            if (client == null)
                return NotFound();

            _db.ApiClients.Remove(client);
            await _db.SaveChangesAsync();

            TempData["Success"] = "API key deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}