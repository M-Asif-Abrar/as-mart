using System.Linq;
using System.Threading.Tasks;
using AsMart.Web.Data;
using AsMart.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var utcToday = DateTime.UtcNow.Date;
            var last7 = utcToday.AddDays(-6);    // inclusive
            var last30 = utcToday.AddDays(-29);  // inclusive

            var vm = new AdminDashboardViewModel
            {
                TotalUsers = await _db.Users.CountAsync(),
                TotalProducts = await _db.Products.CountAsync(),
                TotalCategories = await _db.Categories.CountAsync(),
                TotalBlogPosts = await _db.BlogPosts.CountAsync(),
                TotalCollections = await _db.Collections.CountAsync(),
                TotalClickLogs = await _db.ClickLogs.CountAsync(),
                TotalUserProductStatuses = await _db.UserProductStatuses.CountAsync()
            };

            // -------- Click metrics --------
            vm.ClicksToday = await _db.ClickLogs
                .CountAsync(cl => cl.ClickedAt >= utcToday && cl.ClickedAt < utcToday.AddDays(1));

            vm.ClicksLast7Days = await _db.ClickLogs
                .CountAsync(cl => cl.ClickedAt >= last7 && cl.ClickedAt < utcToday.AddDays(1));

            vm.ClicksLast30Days = await _db.ClickLogs
                .CountAsync(cl => cl.ClickedAt >= last30 && cl.ClickedAt < utcToday.AddDays(1));

            // -------- Products per category (Top 10) --------
            // Use the Category -> ProductCategories navigation; no GroupBy on navigation.
            vm.ProductsPerCategory = await _db.Categories
                .Select(c => new AdminDashboardViewModel.CategoryProductsItem
                {
                    CategoryName = c.Name,
                    ProductCount = c.ProductCategories.Count()
                })
                .OrderByDescending(x => x.ProductCount)
                .Take(10)
                .ToListAsync();

            // -------- Products per collection (Top 10) --------
            vm.ProductsPerCollection = await _db.Collections
                .Select(col => new AdminDashboardViewModel.CollectionProductsItem
                {
                    CollectionName = col.Name,
                    ProductCount = col.CollectionProducts.Count()
                })
                .OrderByDescending(x => x.ProductCount)
                .Take(10)
                .ToListAsync();

            // -------- Top products by clicks (last 30 days) --------
            // First group ClickLogs by ProductId, then join with Products.
            var topProducts = await _db.ClickLogs
                .Where(cl => cl.ClickedAt >= last30 && cl.ProductId != 0)
                .GroupBy(cl => cl.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Clicks = g.Count(),
                    LastClickedAt = g.Max(x => x.ClickedAt)
                })
                .OrderByDescending(x => x.Clicks)
                .Take(10)
                .Join(
                    _db.Products,
                    g => g.ProductId,
                    p => p.Id,
                    (g, p) => new { g, p }
                )
                .Select(x => new AdminDashboardViewModel.TopProductClicksItem
                {
                    ProductId = x.p.Id,
                    Slug = x.p.Slug,
                    Title = x.p.Title,
                    CategoryName = x.p.ProductCategories
                        .Select(pc => pc.Category!.Name)
                        .FirstOrDefault(),
                    Clicks = x.g.Clicks,
                    LastClickedAt = x.g.LastClickedAt
                })
                .ToListAsync();

            vm.TopProductsByClicks = topProducts;

            // -------- Top categories by clicks (last 30 days) --------
            vm.TopCategoriesByClicks = await _db.ClickLogs
                .Where(cl => cl.ClickedAt >= last30)
                .Join(_db.ProductCategories,
                      cl => cl.ProductId,
                      pc => pc.ProductId,
                      (cl, pc) => pc.CategoryId)
                .Join(_db.Categories,
                      cid => cid,
                      c => c.Id,
                      (cid, c) => c.Name)
                .GroupBy(name => name)
                .Select(g => new AdminDashboardViewModel.TopCategoryClicksItem
                {
                    CategoryName = g.Key,
                    Clicks = g.Count()
                })
                .OrderByDescending(x => x.Clicks)
                .Take(10)
                .ToListAsync();

            return View(vm);
        }


    }
}
