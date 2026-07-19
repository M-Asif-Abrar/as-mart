using AsMart.Web.Data;
using AsMart.Web.Models.ViewModels;
using AsMart.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Controllers
{
    [Route("guides")]
    public class GuidesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly SeoProductSelector _selector;

        public GuidesController(
            ApplicationDbContext db,
            SeoProductSelector selector)
        {
            _db = db;
            _selector = selector;
        }

        [HttpGet("{slug}")]
        public IActionResult Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            slug = slug.Trim().ToLowerInvariant();

            var page = _db.SeoPages
                .AsNoTracking()
                .FirstOrDefault(x =>
                    x.Slug == slug &&
                    x.Status == 1);

            if (page == null)
            {
                return NotFound();
            }

            var relatedGuides = new List<
                AsMart.Web.Models.Entities.SeoPage>();

            if (page.CategoryId.HasValue)
            {
                relatedGuides = _db.SeoPages
                    .AsNoTracking()
                    .Where(x =>
                        x.Status == 1 &&
                        x.CategoryId == page.CategoryId &&
                        x.Id != page.Id)
                    .OrderBy(x => Guid.NewGuid())
                    .Take(12)
                    .ToList();
            }

            var vm = new GuideViewModel
            {
                Page = page,

                Products = _selector.Select(
                    page,
                    take: 12),

                RelatedGuides = relatedGuides
            };

            ViewData["Title"] =
                page.Title ?? string.Empty;

            ViewData["MetaDescription"] =
                page.MetaDescription ?? string.Empty;

            return View(
                "~/Views/Guides/Guide.cshtml",
                vm);
        }
    }
}