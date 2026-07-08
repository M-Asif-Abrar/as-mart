// Controllers/ProductController.cs
using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using AsMart.Web.Models.ViewModels;
using AsMart.Web.Services;
using AsMart.Web.Services.Marketing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AsMart.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAffiliateLinkService _affiliate;
        private readonly IUtmTrackingService _utm;

        public ProductController(
            ApplicationDbContext db,
            IAffiliateLinkService affiliate,
            IUtmTrackingService utm)
        {
            _db = db;
            _affiliate = affiliate;
            _utm = utm;
        }

        [HttpGet("/product/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            var product = await _db.Products
                .AsNoTracking()
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

            if (product == null)
                return NotFound();

            await _utm.TrackVisitAsync(HttpContext, productId: product.Id, clickType: "ProductLanding");

            // Pick ONE best category only.
            // Prefer child category, not parent/general category.
            // Example: Smart Watches instead of Electronics.
            var targetCategoryId = product.ProductCategories
                .Where(pc => pc.Category != null)
                .Select(pc => pc.Category!)
                .OrderByDescending(c => c.ParentCategoryId.HasValue)
                .ThenBy(c => c.Name)
                .Select(c => c.Id)
                .FirstOrDefault();

            IReadOnlyList<Product> relatedCategoryProducts = Array.Empty<Product>();
            IReadOnlyList<Product> relatedProducts = Array.Empty<Product>();

            if (targetCategoryId != 0)
            {
                relatedCategoryProducts = await _db.Products
                    .AsNoTracking()
                    .Include(p => p.ProductCategories)
                        .ThenInclude(pc => pc.Category)
                    .Where(p =>
                        p.IsActive &&
                        p.Id != product.Id &&
                        p.ProductCategories.Any(pc => pc.CategoryId == targetCategoryId))
                    .OrderByDescending(p => p.Rating ?? 0)
                    .ThenByDescending(p => p.RatingCount ?? 0)
                    .ThenByDescending(p => p.CreatedAt)
                    .ToListAsync();

                relatedProducts = relatedCategoryProducts
                    .Take(12)
                    .ToList();
            }

            var excludedIds = relatedCategoryProducts
                .Select(p => p.Id)
                .Append(product.Id)
                .ToHashSet();

            var otherProducts = await _db.Products
                .AsNoTracking()
                .Where(p => p.IsActive && !excludedIds.Contains(p.Id))
                .OrderByDescending(p => p.ClickCount)
                .ThenByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.CreatedAt)
                .Take(12)
                .ToListAsync();

            var vm = new ProductDetailViewModel
            {
                Product = product,

                // Bottom carousel
                RelatedProducts = relatedProducts.Select(ToCardVm).ToList(),

                // Left-side image box: SAME category only
                RelatedCategoryProducts = relatedCategoryProducts.Select(ToCardVm).ToList(),

                OtherProducts = otherProducts.Select(ToCardVm).ToList()
            };

            return View(vm);
        }

        private static ProductCardViewModel ToCardVm(Product p)
        {
            var primaryCategory = p.ProductCategories?
                .Select(pc => pc.Category?.Name)
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

            return new ProductCardViewModel
            {
                Id = p.Id,
                Slug = p.Slug,
                Title = p.Title,
                MainImageUrl = p.MainImageUrl,
                Price = p.Price,
                ListPrice = p.ListPrice,
                Currency = p.Currency,
                Rating = p.Rating,
                RatingCount = p.RatingCount,
                PrimaryCategoryName = primaryCategory
            };
        }

        [HttpGet("/product/go/{id:int}")]
        public async Task<IActionResult> Go(int id)
        {
            var product = await _db.Products.FindAsync(id);

            if (product == null || !product.IsActive)
                return NotFound();

            var affiliateUrl = !string.IsNullOrWhiteSpace(product.ASIN)
                ? _affiliate.BuildProductUrl(product.ASIN)
                : product.AffiliateUrlOverride;

            if (string.IsNullOrWhiteSpace(affiliateUrl))
                return RedirectToAction("Details", new { slug = product.Slug });

            string? userId = User?.Identity?.IsAuthenticated == true
                ? User.GetUserId()
                : null;

            var userAgent = Request.Headers["User-Agent"].ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (IsBotLike(userAgent))
                return Redirect(affiliateUrl);

            var since = DateTime.UtcNow.AddSeconds(-60);

            var already = await _db.ClickLogs.AnyAsync(x =>
                x.ProductId == product.Id &&
                x.ClickType == "AmazonOutbound" &&
                x.IPAddress == ip &&
                x.UserAgent == userAgent &&
                x.ClickedAt >= since);

            if (!already)
            {
                var click = new ClickLog
                {
                    ProductId = product.Id,
                    ClickType = "AmazonOutbound",
                    ClickedAt = DateTime.UtcNow,
                    UserId = userId,
                    IPAddress = ip,
                    UserAgent = userAgent
                };

                _db.ClickLogs.Add(click);
                product.ClickCount++;

                if (!string.IsNullOrEmpty(userId))
                {
                    var status = new UserProductStatus
                    {
                        UserId = userId,
                        ProductId = product.Id,
                        State = UserProductState.Clicked,
                        CreatedAt = DateTime.UtcNow
                    };

                    _db.UserProductStatuses.Add(status);
                }

                await _db.SaveChangesAsync();
            }

            return Redirect(affiliateUrl);
        }

        private static bool IsBotLike(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                return true;

            var ua = userAgent.ToLowerInvariant();

            return ua.Contains("bot") ||
                   ua.Contains("crawler") ||
                   ua.Contains("spider") ||
                   ua.Contains("slurp") ||
                   ua.Contains("headless") ||
                   ua.Contains("lighthouse") ||
                   ua.Contains("curl") ||
                   ua.Contains("wget") ||
                   ua.Contains("python") ||
                   ua.Contains("scrapy");
        }
    }

    public static class IdentityExtensions
    {
        public static string? GetUserId(this System.Security.Claims.ClaimsPrincipal user)
        {
            return user.Claims
                .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;
        }
    }
}