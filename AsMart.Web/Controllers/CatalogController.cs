// Controllers/CatalogController.cs
using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using AsMart.Web.Models.ViewModels;
using AsMart.Web.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Controllers
{
    [Route("[controller]")]
    public class CatalogController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ICategoryRepository _categoryRepository;

        public CatalogController(ApplicationDbContext db, ICategoryRepository categoryRepository)
        {
            _db = db;
            _categoryRepository = categoryRepository;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 24,
            string? categories = null,
            string? brand = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            decimal? minRating = null,
            decimal? maxRating = null)
        {
            var selectedCategoryIds = ParseCategoryIds(categories);
            brand = NormalizeBrand(brand);

            IQueryable<Product> query = _db.Products
                .AsNoTracking()
                .Where(p => p.IsActive);

            if (selectedCategoryIds.Count > 0)
            {
                query = query.Where(p =>
                    p.ProductCategories.Any(pc => selectedCategoryIds.Contains(pc.CategoryId)));
            }

            if (!string.IsNullOrWhiteSpace(brand))
            {
                query = query.Where(p => p.Brand != null && p.Brand.Trim() == brand);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price.HasValue && p.Price.Value >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price.HasValue && p.Price.Value <= maxPrice.Value);
            }

            if (minRating.HasValue)
            {
                query = query.Where(p => p.Rating.HasValue && p.Rating.Value >= minRating.Value);
            }

            if (maxRating.HasValue)
            {
                query = query.Where(p => p.Rating.HasValue && p.Rating.Value <= maxRating.Value);
            }

            query = query.OrderByDescending(p => p.CreatedAt);

            var totalCount = await query.CountAsync();

            if (page < 1)
                page = 1;

            var maxPage = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (maxPage > 0 && page > maxPage)
                page = maxPage;

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new Product
                {
                    Id = p.Id,
                    ASIN = p.ASIN,
                    Title = p.Title,
                    Slug = p.Slug,
                    ShortDescription = p.ShortDescription,
                    Description = p.Description,
                    Brand = p.Brand,
                    Price = p.Price,
                    ListPrice = p.ListPrice,
                    Currency = p.Currency,
                    Rating = p.Rating,
                    RatingCount = p.RatingCount,
                    MainImageUrl = p.MainImageUrl,
                    AdditionalImagesJson = p.AdditionalImagesJson,
                    AffiliateUrlOverride = p.AffiliateUrlOverride,
                    IsFeatured = p.IsFeatured,
                    IsActive = p.IsActive,
                    IsDealOfTheDay = p.IsDealOfTheDay,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    LastSyncedAt = p.LastSyncedAt,
                    ClickCount = p.ClickCount
                })
                .ToListAsync();

            var allCategories = await _categoryRepository.GetActiveCategoriesForCatalogAsync();
            var categoryCounts = await _categoryRepository.GetCategoryCountsForCatalogAsync();
            var brandOptions = await _categoryRepository.GetBrandOptionsForCatalogAsync();

            var model = new CatalogIndexViewModel
            {
                Products = products,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                AllBrands = brandOptions.Select(x => x.Name).ToList(),
                BrandOptions = brandOptions,
                Brand = brand,
                AllCategories = allCategories,
                CategoryCounts = categoryCounts,
                SelectedCategoryIds = selectedCategoryIds,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinRating = minRating,
                MaxRating = maxRating
            };

            return View(model);
        }

        [HttpGet("Category")]
        public async Task<IActionResult> Category(string slug)
        {
            var category = await _categoryRepository.GetBySlugAsync(slug);

            if (category == null)
                return NotFound();

            var (products, totalCount) = await _categoryRepository.GetPagedProductsByCategorySlugAsync(slug);

            var model = new CatalogCategoryViewModel
            {
                Category = category,
                Products = products,
                Page = 1,
                PageSize = totalCount,
                TotalCount = totalCount
            };

            return View(model);
        }

        [HttpGet("Search")]
        public async Task<IActionResult> Search(string? q, int page = 1, int pageSize = 24)
        {
            q ??= "";
            var raw = q.Trim();

            if (string.IsNullOrWhiteSpace(raw))
            {
                var baseQuery = _db.Products.AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.CreatedAt);

                var total0 = await baseQuery.CountAsync();
                var list0 = await baseQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

                return View(new CatalogSearchViewModel
                {
                    Query = raw,
                    Products = list0,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = total0
                });
            }

            static string Normalize(string s)
            {
                s = s.Trim().ToLowerInvariant();
                var sb = new System.Text.StringBuilder(s.Length);
                var lastSpace = false;

                foreach (var ch in s)
                {
                    if (char.IsLetterOrDigit(ch))
                    {
                        sb.Append(ch);
                        lastSpace = false;
                    }
                    else if (!lastSpace)
                    {
                        sb.Append(' ');
                        lastSpace = true;
                    }
                }

                return sb.ToString().Trim();
            }

            static IEnumerable<string> ExpandTokens(IEnumerable<string> tokens)
            {
                foreach (var t in tokens)
                {
                    yield return t;

                    if (t.Length > 4 && t.EndsWith("ies"))
                        yield return t[..^3] + "y";
                    else if (t.Length > 3 && t.EndsWith("es"))
                        yield return t[..^2];
                    else if (t.Length > 2 && t.EndsWith("s"))
                        yield return t[..^1];
                }
            }

            var qNorm = Normalize(raw);
            var tokens = qNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
            var expandedTokens = ExpandTokens(tokens).Distinct().ToArray();

            var baseSql = _db.Products.AsNoTracking()
                .Where(p => p.IsActive);

            foreach (var t in expandedTokens)
            {
                var tok = t;
                baseSql = baseSql.Where(p =>
                    (p.Title != null && EF.Functions.Like(p.Title, "%" + tok + "%")) ||
                    (p.Brand != null && EF.Functions.Like(p.Brand, "%" + tok + "%")) ||
                    (p.ShortDescription != null && EF.Functions.Like(p.ShortDescription, "%" + tok + "%")));
            }

            var totalCount = await baseSql.CountAsync();

            var products = await baseSql
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (products.Count == 0)
            {
                var relaxed = _db.Products.AsNoTracking().Where(p => p.IsActive);

                foreach (var t in expandedTokens)
                {
                    var tok = t;
                    relaxed = relaxed.Where(p =>
                        (p.Title != null && EF.Functions.Like(p.Title, "%" + tok + "%")) ||
                        (p.Brand != null && EF.Functions.Like(p.Brand, "%" + tok + "%")) ||
                        (p.ShortDescription != null && EF.Functions.Like(p.ShortDescription, "%" + tok + "%")));
                }

                products = await relaxed
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                totalCount = await relaxed.CountAsync();
            }

            return View(new CatalogSearchViewModel
            {
                Query = raw,
                Products = products,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        private static List<int> ParseCategoryIds(string? categories)
        {
            if (string.IsNullOrWhiteSpace(categories))
                return new List<int>();

            return categories
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => int.TryParse(x, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }

        private static string? NormalizeBrand(string? brand)
        {
            return string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
        }
    }
}