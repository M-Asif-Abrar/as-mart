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

        // ----------------------------------------------------
        // PRODUCT DETAILS + RELATED / OTHER PRODUCTS
        // ----------------------------------------------------
        [HttpGet("/product/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            // main product
            var product = await _db.Products
                .AsNoTracking()
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

            if (product == null)
                return NotFound();


            await _utm.TrackVisitAsync(HttpContext, productId: product.Id, clickType: "ProductLanding");

            // ---------------- RELATED PRODUCTS ----------------
            // collect this product's own categories + parent categories
            var categoryIds = product.ProductCategories
                .Where(pc => pc.Category != null)
                .Select(pc => pc.Category!)
                .SelectMany(c => new[]
                {
                    c.Id,
                    c.ParentCategoryId ?? 0
                })
                .Where(id => id != 0)
                .Distinct()
                .ToList();

            IReadOnlyList<Product> relatedProducts = Array.Empty<Product>();

            if (categoryIds.Any())
            {
                var relatedQuery = _db.Products
                    .AsNoTracking()
                    .Include(p => p.ProductCategories)
                        .ThenInclude(pc => pc.Category)
                    .Where(p =>
                        p.IsActive &&
                        p.Id != product.Id &&
                        p.ProductCategories.Any(pc => categoryIds.Contains(pc.CategoryId)));

                // score: number of shared categories → rating → rating count
                relatedQuery = relatedQuery
                    .OrderByDescending(p =>
                        p.ProductCategories.Count(pc => categoryIds.Contains(pc.CategoryId)))
                    .ThenByDescending(p => p.Rating ?? 0)
                    .ThenByDescending(p => p.RatingCount ?? 0);

                relatedProducts = await relatedQuery.Take(12).ToListAsync();
            }

            // ---------------- OTHER PRODUCTS ----------------
            // exclude current + already-related
            var excludedIds = relatedProducts
                .Select(p => p.Id)
                .Append(product.Id)
                .ToHashSet();

            var otherQuery = _db.Products
                .AsNoTracking()
                .Where(p => p.IsActive && !excludedIds.Contains(p.Id));

            // "interesting" items – clicks / rating / recency
            otherQuery = otherQuery
                .OrderByDescending(p => p.ClickCount)
                .ThenByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.CreatedAt);

            var otherProducts = await otherQuery.Take(12).ToListAsync();

            // ---------------- BUILD VIEWMODEL ----------------
            var vm = new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = relatedProducts.Select(ToCardVm).ToList(),
                OtherProducts = otherProducts.Select(ToCardVm).ToList()
            };

            return View(vm);
        }

        // maps Product → ProductCardViewModel for carousels
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

        // ----------------------------------------------------
        // AFFILIATE REDIRECT + CLICK LOGGING
        // ----------------------------------------------------
        [HttpGet("/product/go/{id:int}")]
        public async Task<IActionResult> Go(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null || !product.IsActive)
                return NotFound();

            var affiliateUrl = !string.IsNullOrWhiteSpace(product.AffiliateUrlOverride)
                ? product.AffiliateUrlOverride
                : _affiliate.BuildProductUrl(product.ASIN);

            // Get current user id if authenticated
            string? userId = User?.Identity?.IsAuthenticated == true
                ? User.GetUserId()
                : null;

            // ---------- STEP 3 FIX: bot filter + dedup ----------
            var userAgent = Request.Headers["User-Agent"].ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            // If bot-like traffic, do NOT log (prevents inflated counts)
            if (IsBotLike(userAgent))
                return Redirect(affiliateUrl);

            // Deduplicate within 60 seconds for same product + IP + UA + click type
            var since = DateTime.UtcNow.AddSeconds(-60);

            var already = await _db.ClickLogs.AnyAsync(x =>
                x.ProductId == product.Id &&
                x.ClickType == "AmazonOutbound" &&
                x.IPAddress == ip &&
                x.UserAgent == userAgent &&
                x.ClickedAt >= since);

            if (!already)
            {
                // Log click (only if not duplicate)
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

                // increment product click counter once per dedup window
                product.ClickCount++;

                // If user is logged in, insert a UserProductStatus (Clicked)
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

        // simple UA filter for non-human traffic
        private static bool IsBotLike(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent)) return true;

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

    // --------------------------------------------------------
    // IDENTITY EXTENSION: GET CURRENT USER ID
    // --------------------------------------------------------
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
