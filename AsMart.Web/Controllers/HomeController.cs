// Controllers/HomeController.cs
using AsMart.Web.Data;
using AsMart.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // SEO (Home Page)
            ViewData["Title"] = "As-Mart – Best Amazon Deals on Electronics, Kids & Gadgets";
            ViewData["MetaDescription"] =
                "As-Mart is an Amazon affiliate store featuring top deals on electronics, kids products, gadgets, tablets, laptops, and daily essentials. Compare prices and buy smart.";

            var featured = await _db.Products
                .Where(p => p.IsActive && p.IsFeatured)
                .OrderByDescending(p => p.CreatedAt)
                .Take(12)
                .ToListAsync();

            var deals = await _db.Products
                .Where(p => p.IsActive && p.IsDealOfTheDay)
                .OrderByDescending(p => p.UpdatedAt)
                .Take(12)
                .ToListAsync();

            var model = new HomeIndexViewModel
            {
                Featured = featured,
                Deals = deals
            };

            return View(model);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Artical()
        {
            return View();
        }

        [HttpGet("/api-documentation")]
        public IActionResult ApiDocumentation()
        {
            ViewData["Title"] = "As-Mart Public Product API Documentation";
            return View();
        }

    }
}
