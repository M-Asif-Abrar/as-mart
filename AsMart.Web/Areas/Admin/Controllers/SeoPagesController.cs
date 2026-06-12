// Areas/Admin/Controllers/SeoPagesController.cs
using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using AsMart.Web.Models.Seo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AsMart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class SeoPagesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public SeoPagesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // REMOVE THIS METHOD OR RENAME IT (it causes AmbiguousMatch)
        // public async Task<IActionResult> Index(string q = null, byte? status = null) { ... }

        [HttpGet]
        public async Task<IActionResult> Index(
            string q = null,
            byte? status = null,
            int? categoryId = null,
            string brand = null,
            string templateKey = null,
            string sortMode = null,
            decimal? priceMin = null,
            decimal? priceMax = null,
            int page = 1,
            int pageSize = 25)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 10 or > 200 ? 25 : pageSize;

            var query = _db.SeoPages.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(x =>
                    (x.Slug != null && x.Slug.Contains(q)) ||
                    (x.Title != null && x.Title.Contains(q)) ||
                    (x.TargetKeyword != null && x.TargetKeyword.Contains(q)));
            }

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            if (categoryId.HasValue)
                query = query.Where(x => x.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(brand))
            {
                brand = brand.Trim();
                query = query.Where(x => x.Brand != null && x.Brand.Contains(brand));
            }

            if (!string.IsNullOrWhiteSpace(templateKey))
            {
                templateKey = templateKey.Trim();
                query = query.Where(x => x.TemplateKey != null && x.TemplateKey.Contains(templateKey));
            }

            if (!string.IsNullOrWhiteSpace(sortMode))
            {
                sortMode = sortMode.Trim();
                query = query.Where(x => x.SortMode != null && x.SortMode.Contains(sortMode));
            }

            if (priceMin.HasValue)
                query = query.Where(x => x.PriceMin == null || x.PriceMin >= priceMin.Value);

            if (priceMax.HasValue)
                query = query.Where(x => x.PriceMax == null || x.PriceMax <= priceMax.Value);

            var total = await query.CountAsync();

            // Areas/Admin/Controllers/SeoPagesController.cs  (only the items query part)
            var items = await query
                .OrderByDescending(x => x.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SeoPagesIndexRowVm
                {
                    Id = x.Id,
                    Slug = x.Slug,
                    Title = x.Title,
                    TargetKeyword = x.TargetKeyword,
                    Status = x.Status,
                    UpdatedAt = x.UpdatedAt,
                    CategoryId = x.CategoryId,
                    CategoryName = _db.Categories
                        .Where(c => c.Id == x.CategoryId)
                        .Select(c => c.Name)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var categories = await _db.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.ParentCategoryId)
                .ThenBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);

            var meta = new SeoPagesIndexMeta
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Q = q,
                Status = status,
                CategoryId = categoryId,
                Brand = brand,
                TemplateKey = templateKey,
                SortMode = sortMode,
                PriceMin = priceMin,
                PriceMax = priceMax
            };

            return View(new SeoPagesIndexVm { Items = items, Meta = meta });
        }

        public IActionResult Create()
        {
            var model = new SeoPage
            {
                SortMode = "rank",
                Status = 0,
                UpdatedAt = DateTime.UtcNow
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SeoPage model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.Slug = (model.Slug ?? "").Trim().ToLowerInvariant();
            model.UpdatedAt = DateTime.UtcNow;

            if (model.Status == 1 && model.PublishedAt == null)
                model.PublishedAt = DateTime.UtcNow;

            _db.SeoPages.Add(model);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var page = await _db.SeoPages.FirstOrDefaultAsync(x => x.Id == id);
            if (page == null) return NotFound();

            return View(page);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SeoPage model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var page = await _db.SeoPages.FirstOrDefaultAsync(x => x.Id == id);
            if (page == null) return NotFound();

            page.Slug = (model.Slug ?? "").Trim().ToLowerInvariant();
            page.Title = model.Title;
            page.MetaDescription = model.MetaDescription;
            page.H1 = model.H1;

            page.TemplateKey = model.TemplateKey;
            page.TargetKeyword = model.TargetKeyword;

            page.CategoryId = model.CategoryId;
            page.Brand = model.Brand;
            page.PriceMin = model.PriceMin;
            page.PriceMax = model.PriceMax;
            page.RulesJson = model.RulesJson;

            page.IntroHtml = model.IntroHtml;
            page.BodyHtml = model.BodyHtml;
            page.FaqJson = model.FaqJson;

            page.SortMode = string.IsNullOrWhiteSpace(model.SortMode) ? "rank" : model.SortMode.Trim();
            page.Status = model.Status;

            if (page.Status == 1 && page.PublishedAt == null)
                page.PublishedAt = DateTime.UtcNow;

            page.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            var page = await _db.SeoPages.FirstOrDefaultAsync(x => x.Id == id);
            if (page == null) return NotFound();

            page.Status = 1;
            page.PublishedAt ??= DateTime.UtcNow;
            page.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unpublish(int id)
        {
            var page = await _db.SeoPages.FirstOrDefaultAsync(x => x.Id == id);
            if (page == null) return NotFound();

            page.Status = 0;
            page.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var page = await _db.SeoPages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (page == null) return NotFound();

            return View(page);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var page = await _db.SeoPages.FirstOrDefaultAsync(x => x.Id == id);
            if (page == null) return NotFound();

            var snaps = _db.SeoPageProductSnapshots.Where(x => x.SeoPageId == id);
            _db.SeoPageProductSnapshots.RemoveRange(snaps);

            _db.SeoPages.Remove(page);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}