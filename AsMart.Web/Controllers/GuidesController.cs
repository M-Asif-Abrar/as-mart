    using AsMart.Web.Data;
using AsMart.Web.Models.ViewModels;
using AsMart.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace AsMart.Web.Controllers
{
    [Route("guides")]
    public class GuidesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly SeoProductSelector _selector;

        public GuidesController(ApplicationDbContext db, SeoProductSelector selector)
        {
            _db = db;
            _selector = selector;
        }

        [HttpGet("{slug}")]
        public IActionResult Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return NotFound();

            slug = slug.Trim().ToLowerInvariant();

            var page = _db.SeoPages.FirstOrDefault(x => x.Slug == slug && x.Status == 1);
            if (page == null)
                return NotFound();

            var vm = new GuideViewModel
            {
                Page = page,
                Products = _selector.Select(page, take: 12)
            };

            ViewData["Title"] = page.Title ?? "";
            ViewData["MetaDescription"] = page.MetaDescription ?? "";

            return View("~/Views/Guides/Guide.cshtml", vm);
        }
    }
}
