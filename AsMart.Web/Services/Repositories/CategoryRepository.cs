// Services/Repositories/CategoryRepository.cs
using System.Text.Json;
using System.Text.RegularExpressions;
using AsMart.Web.Data;
using AsMart.Web.Models.DTOs;
using AsMart.Web.Models.Entities;
using AsMart.Web.Models.ViewModels;
using AsMart.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Services.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly ISlugService _slugService;

        public CategoryRepository(ApplicationDbContext db, ISlugService slugService)
        {
            _db = db;
            _slugService = slugService;
        }

        private sealed class CategoryBadgeLink
        {
            public string Url { get; set; } = "";
            public string Text { get; set; } = "";
        }

        private static readonly Regex AnchorRegex = new(
            @"<a\s+[^>]*href\s*=\s*[""'](?<href>[^""']+)[""'][^>]*>(?<text>.*?)</a>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

        private static string? NormalizeBadgesToJson(string? multiline)
        {
            if (string.IsNullOrWhiteSpace(multiline))
                return null;

            var lines = multiline
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => (x ?? "").Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            var items = new List<CategoryBadgeLink>();

            foreach (var raw in lines)
            {
                var line = raw.Trim();

                if (line.Contains("<a", StringComparison.OrdinalIgnoreCase) &&
                    line.Contains("href", StringComparison.OrdinalIgnoreCase))
                {
                    var m = AnchorRegex.Match(line);
                    if (m.Success)
                    {
                        var href = (m.Groups["href"].Value ?? "").Trim();
                        var text = Regex.Replace(m.Groups["text"].Value ?? "", "<.*?>", string.Empty).Trim();

                        if (!string.IsNullOrWhiteSpace(href) && !string.IsNullOrWhiteSpace(text))
                            items.Add(new CategoryBadgeLink { Url = href, Text = text });
                    }

                    continue;
                }

                var parts = line.Split('|', 2, StringSplitOptions.None)
                    .Select(x => (x ?? "").Trim())
                    .ToArray();

                if (parts.Length == 2)
                {
                    var text = parts[0];
                    var url = parts[1];

                    if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(url))
                        items.Add(new CategoryBadgeLink { Url = url, Text = text });

                    continue;
                }

                if (line.StartsWith("/", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var url = line;
                    var slug = url.TrimEnd('/').Split('/').LastOrDefault() ?? url;
                    var text = slug.Replace('-', ' ').Trim();

                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        items.Add(new CategoryBadgeLink
                        {
                            Url = url,
                            Text = string.IsNullOrWhiteSpace(text) ? url : ToTitle(text)
                        });
                    }
                }
            }

            var normalized = items
                .Where(x => !string.IsNullOrWhiteSpace(x.Url) && !string.IsNullOrWhiteSpace(x.Text))
                .Select(x => new CategoryBadgeLink
                {
                    Url = x.Url.Trim(),
                    Text = x.Text.Trim()
                })
                .GroupBy(x => x.Url, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            if (normalized.Count == 0)
                return null;

            return JsonSerializer.Serialize(normalized);
        }

        private static string? JsonToBadgesMultiline(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                var items = JsonSerializer.Deserialize<List<CategoryBadgeLink>>(json) ?? new List<CategoryBadgeLink>();

                var normalized = items
                    .Select(x => new CategoryBadgeLink
                    {
                        Url = (x.Url ?? "").Trim(),
                        Text = (x.Text ?? "").Trim()
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Url) && !string.IsNullOrWhiteSpace(x.Text))
                    .GroupBy(x => x.Url, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

                if (normalized.Count == 0)
                    return null;

                return string.Join("\n", normalized.Select(x => $"{x.Text} | {x.Url}"));
            }
            catch
            {
                return null;
            }
        }

        private static string ToTitle(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return s;

            var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                var p = parts[i];
                parts[i] = p.Length == 1 ? p.ToUpperInvariant() : char.ToUpperInvariant(p[0]) + p[1..];
            }

            return string.Join(' ', parts);
        }

        public async Task<List<CategoryListItemDto>> GetAllAsync()
        {
            var categories = await _db.Categories
                .AsNoTracking()
                .Include(c => c.ParentCategory)
                .Include(c => c.ProductCategories)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return categories.Select(c => new CategoryListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                ParentCategoryName = c.ParentCategory?.Name,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive,
                ProductCount = c.ProductCategories.Count,
                LinksCount = CountBadges(c.Links)
            }).ToList();
        }

        public async Task<CategoryDetailsDto?> GetDetailsAsync(int id)
        {
            var category = await _db.Categories
                .AsNoTracking()
                .Include(c => c.ParentCategory)
                .Include(c => c.Children)
                .Include(c => c.ProductCategories)
                    .ThenInclude(pc => pc.Product)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return null;

            var badges = ParseBadges(category.Links);

            var products = (category.ProductCategories ?? new List<ProductCategory>())
                .Select(pc => pc.Product)
                .Where(p => p != null && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new CategoryProductThumbDto
                {
                    Id = p.Id,
                    Slug = p.Slug ?? "",
                    Title = p.Title ?? "",
                    MainImageUrl = p.MainImageUrl
                })
                .Take(24)
                .ToList();

            return new CategoryDetailsDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                ParentCategoryName = category.ParentCategory?.Name,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive,
                ChildCount = category.Children?.Count ?? 0,
                ProductCount = category.ProductCategories?.Count ?? 0,
                LinksCount = badges.Count,
                Links = badges.Select(x => new CategoryBadgeLinkDto
                {
                    Url = x.Url,
                    Text = x.Text
                }).ToList(),
                Products = products
            };
        }

        public async Task<CategoryFormDto?> GetForEditAsync(int id)
        {
            var category = await _db.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return null;

            return new CategoryFormDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive,
                Links = JsonToBadgesMultiline(category.Links)
            };
        }

        public async Task<int> CreateAsync(CategoryFormDto dto)
        {
            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                ParentCategoryId = dto.ParentCategoryId,
                DisplayOrder = dto.DisplayOrder,
                IsActive = dto.IsActive,
                Links = NormalizeBadgesToJson(dto.Links)
            };

            var slugBase = !string.IsNullOrWhiteSpace(dto.Slug)
                ? dto.Slug
                : _slugService.GenerateSlug(dto.Name);

            category.Slug = await GenerateUniqueSlugAsync(slugBase);

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return category.Id;
        }

        public async Task UpdateAsync(CategoryFormDto dto)
        {
            if (!dto.Id.HasValue)
                throw new ArgumentException("Category id is required for update.");

            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == dto.Id.Value);

            if (category == null)
                throw new InvalidOperationException("Category not found.");

            category.Name = dto.Name;
            category.Description = dto.Description;
            category.ParentCategoryId = dto.ParentCategoryId;
            category.DisplayOrder = dto.DisplayOrder;
            category.IsActive = dto.IsActive;
            category.Links = NormalizeBadgesToJson(dto.Links);

            var newSlugBase = !string.IsNullOrWhiteSpace(dto.Slug)
                ? dto.Slug
                : _slugService.GenerateSlug(dto.Name);

            if (!string.Equals(newSlugBase, category.Slug, StringComparison.OrdinalIgnoreCase))
                category.Slug = await GenerateUniqueSlugAsync(newSlugBase, category.Id);

            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _db.Categories
                .Include(c => c.Children)
                .Include(c => c.ProductCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return;

            if (category.Children.Any())
                throw new InvalidOperationException("Cannot delete category with child categories. Reassign or remove children first.");

            if (category.ProductCategories.Any())
                throw new InvalidOperationException("Cannot delete category with products assigned. Reassign or remove products first.");

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
        }

        public Task<bool> ExistsAsync(int id)
        {
            return _db.Categories.AnyAsync(c => c.Id == id);
        }

        public async Task<List<Category>> GetAllEntitiesAsync()
        {
            return await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<Category>> GetActiveCategoriesForCatalogAsync()
        {
            return await _db.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new Category
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug
                })
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Dictionary<int, int>> GetCategoryCountsForCatalogAsync()
        {
            return await _db.ProductCategories
                .AsNoTracking()
                .GroupBy(pc => pc.CategoryId)
                .Select(g => new
                {
                    CategoryId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.CategoryId, x => x.Count);
        }

        public async Task<List<BrandFilterOptionVm>> GetBrandOptionsForCatalogAsync()
        {
            var rows = await _db.Products
                .AsNoTracking()
                .Where(p => p.IsActive && p.Brand != null && p.Brand.Trim() != "")
                .SelectMany(p => p.ProductCategories.Select(pc => new
                {
                    Brand = p.Brand!.Trim(),
                    CategoryName = pc.Category.Name,
                    ProductId = p.Id
                }))
                .GroupBy(x => new { x.Brand, x.CategoryName })
                .Select(g => new
                {
                    g.Key.Brand,
                    g.Key.CategoryName,
                    Count = g.Select(x => x.ProductId).Distinct().Count()
                })
                .ToListAsync();

            return rows
                .GroupBy(x => x.Brand)
                .OrderBy(g => g.Key)
                .Select(g => new BrandFilterOptionVm
                {
                    Name = g.Key,
                    Categories = g
                        .OrderBy(x => x.CategoryName)
                        .Select(x => new BrandFilterCategoryVm
                        {
                            Name = x.CategoryName,
                            Count = x.Count
                        })
                        .ToList()
                })
                .ToList();
        }

        public async Task<Category?> GetBySlugAsync(string slug)
        {
            return await _db.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);
        }

        public async Task<(List<Product> Products, int TotalCount)> GetPagedProductsByCategorySlugAsync(string slug)
        {
            var products = await _db.Products
                .AsNoTracking()
                .Where(p => p.IsActive && p.ProductCategories.Any(pc => pc.Category.Slug == slug && pc.Category.IsActive))
                .OrderBy(p => p.CreatedAt)
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

            return (products, products.Count);
        }

        private async Task<string> GenerateUniqueSlugAsync(string slugBase, int? excludeId = null)
        {
            var slug = slugBase;
            var originalSlug = slugBase;
            var i = 1;

            while (await _db.Categories.AnyAsync(c => c.Slug == slug && (!excludeId.HasValue || c.Id != excludeId.Value)))
                slug = $"{originalSlug}-{i++}";

            return slug;
        }

        private static List<CategoryBadgeLink> ParseBadges(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<CategoryBadgeLink>();

            try
            {
                var items = JsonSerializer.Deserialize<List<CategoryBadgeLink>>(json) ?? new List<CategoryBadgeLink>();

                return items
                    .Select(x => new CategoryBadgeLink
                    {
                        Url = (x.Url ?? "").Trim(),
                        Text = (x.Text ?? "").Trim()
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Url) && !string.IsNullOrWhiteSpace(x.Text))
                    .GroupBy(x => x.Url, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();
            }
            catch
            {
                return new List<CategoryBadgeLink>();
            }
        }

        private static int CountBadges(string? json) => ParseBadges(json).Count;
    }
}