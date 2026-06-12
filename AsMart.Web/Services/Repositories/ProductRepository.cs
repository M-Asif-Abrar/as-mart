using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AsMart.Web.Data;
using AsMart.Web.Models.DTOs;
using AsMart.Web.Models.Entities;
using AsMart.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Services.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly ISlugService _slugService;

        public ProductRepository(ApplicationDbContext db, ISlugService slugService)
        {
            _db = db;
            _slugService = slugService;
        }

        public async Task<List<ProductListItemDto>> GetAllAsync()
        {
            return await _db.Products
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductListItemDto
                {
                    Id = p.Id,
                    ASIN = p.ASIN,
                    Title = p.Title,
                    Slug = p.Slug,
                    Price = p.Price,
                    Currency = p.Currency,
                    IsActive = p.IsActive,
                    IsFeatured = p.IsFeatured,
                    IsDealOfTheDay = p.IsDealOfTheDay,
                    CreatedAt = p.CreatedAt,
                    MainImageUrl = p.MainImageUrl,
                    ClickCount = p.ClickCount
                })
                .ToListAsync();
        }

        public async Task<ProductDetailsDto?> GetDetailsAsync(int id)
        {
            var product = await _db.Products
                .AsNoTracking()
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return null;

            return new ProductDetailsDto
            {
                Id = product.Id,
                ASIN = product.ASIN,
                Title = product.Title,
                Slug = product.Slug,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                Brand = product.Brand,
                Price = product.Price,
                ListPrice = product.ListPrice,
                Currency = product.Currency,
                MainImageUrl = product.MainImageUrl,
                AdditionalImagesJson = product.AdditionalImagesJson,
                AffiliateUrlOverride = product.AffiliateUrlOverride,
                IsFeatured = product.IsFeatured,
                IsDealOfTheDay = product.IsDealOfTheDay,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                ClickCount = product.ClickCount,
                CategoryNames = product.ProductCategories
                    .Select(pc => pc.Category.Name)
                    .OrderBy(n => n)
                    .ToList()
                // if you later add AdditionalImages to ProductDetailsDto,
                // you can map product.AdditionalImagesJson here as well.
            };
        }

        public async Task<ProductFormDto?> GetForEditAsync(int id)
        {
            var product = await _db.Products
                .Include(p => p.ProductCategories)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return null;

            var dto = new ProductFormDto
            {
                Id = product.Id,
                ASIN = product.ASIN,
                Title = product.Title,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                Brand = product.Brand,
                Price = product.Price,
                Rating = product.Rating,
                RatingCount = product.RatingCount,
                ListPrice = product.ListPrice,
                Currency = product.Currency,
                MainImageUrl = product.MainImageUrl,
                AffiliateUrlOverride = product.AffiliateUrlOverride,
                IsFeatured = product.IsFeatured,
                IsDealOfTheDay = product.IsDealOfTheDay,
                IsActive = product.IsActive,
                SelectedCategoryIds = product.ProductCategories
                    .Select(pc => pc.CategoryId)
                    .ToList()
            };

            // Map AdditionalImagesJson -> AdditionalImageUrls (textarea friendly)
            if (!string.IsNullOrWhiteSpace(product.AdditionalImagesJson))
            {
                try
                {
                    var images = JsonSerializer
                        .Deserialize<List<string>>(product.AdditionalImagesJson)
                        ?? new List<string>();

                    // one URL per line for easy editing
                    dto.AdditionalImageUrls = string.Join(Environment.NewLine, images);
                }
                catch
                {
                    // if JSON is somehow corrupted, show raw value
                    dto.AdditionalImageUrls = product.AdditionalImagesJson;
                }
            }

            return dto;
        }

        public async Task<int> CreateAsync(ProductFormDto dto)
        {
            var product = new Product
            {
                ASIN = dto.ASIN,
                Title = dto.Title,
                ShortDescription = dto.ShortDescription,
                Description = dto.Description,
                Brand = dto.Brand,
                Price = dto.Price,
                ListPrice = dto.ListPrice,
                Currency = dto.Currency,
                MainImageUrl = dto.MainImageUrl,
                AffiliateUrlOverride = dto.AffiliateUrlOverride,
                IsFeatured = dto.IsFeatured,
                IsDealOfTheDay = dto.IsDealOfTheDay,
                IsActive = dto.IsActive,
                Rating = dto.Rating,
                RatingCount = dto.RatingCount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // AdditionalImagesJson from AdditionalImageUrls
            product.AdditionalImagesJson = BuildAdditionalImagesJson(dto.AdditionalImageUrls);

            // generate slug
            product.Slug = _slugService.GenerateSlug(product.Title);

            var originalSlug = product.Slug;
            int i = 1;
            while (await _db.Products.AnyAsync(p => p.Slug == product.Slug))
            {
                product.Slug = $"{originalSlug}-{i++}";
            }

            // categories
            foreach (var categoryId in dto.SelectedCategoryIds.Distinct())
            {
                product.ProductCategories.Add(new ProductCategory
                {
                    CategoryId = categoryId,
                    Product = product
                });
            }

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            return product.Id;
        }

        public async Task UpdateAsync(ProductFormDto dto)
        {
            if (!dto.Id.HasValue)
                throw new ArgumentException("Product id is required for update.");

            var product = await _db.Products
                .Include(p => p.ProductCategories)
                .FirstOrDefaultAsync(p => p.Id == dto.Id.Value);

            if (product == null)
                throw new InvalidOperationException("Product not found.");

            product.ASIN = dto.ASIN;
            product.Title = dto.Title;
            product.ShortDescription = dto.ShortDescription;
            product.Description = dto.Description;
            product.Brand = dto.Brand;
            product.Price = dto.Price;
            product.Rating = dto.Rating;
            product.RatingCount = dto.RatingCount;
            product.ListPrice = dto.ListPrice;
            product.Currency = dto.Currency;
            product.MainImageUrl = dto.MainImageUrl;
            product.AffiliateUrlOverride = dto.AffiliateUrlOverride;
            product.IsFeatured = dto.IsFeatured;
            product.IsDealOfTheDay = dto.IsDealOfTheDay;
            product.IsActive = dto.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            // AdditionalImagesJson from AdditionalImageUrls
            product.AdditionalImagesJson = BuildAdditionalImagesJson(dto.AdditionalImageUrls);

            // Re-generate slug if title changed
            var newSlug = _slugService.GenerateSlug(product.Title);
            if (!string.Equals(newSlug, product.Slug, StringComparison.OrdinalIgnoreCase))
            {
                var originalSlug = newSlug;
                int i = 1;
                while (await _db.Products.AnyAsync(p => p.Slug == newSlug && p.Id != product.Id))
                {
                    newSlug = $"{originalSlug}-{i++}";
                }

                product.Slug = newSlug;
            }

            // Update categories
            var newCategoryIds = dto.SelectedCategoryIds.Distinct().ToList();

            // Remove old
            product.ProductCategories.Clear();

            // Add new
            foreach (var categoryId in newCategoryIds)
            {
                product.ProductCategories.Add(new ProductCategory
                {
                    ProductId = product.Id,
                    CategoryId = categoryId
                });
            }

            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _db.Products
                .Include(p => p.ProductCategories)
                .Include(p => p.CollectionProducts)
                .Include(p => p.ClickLogs)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return;

            // Remove related entities first (if cascade not configured)
            _db.ProductCategories.RemoveRange(product.ProductCategories);
            _db.CollectionProducts.RemoveRange(product.CollectionProducts);
            _db.ClickLogs.RemoveRange(product.ClickLogs);

            _db.Products.Remove(product);

            await _db.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _db.Products.AnyAsync(p => p.Id == id);
        }

        public async Task<Product?> GetEntityByIdAsync(int id)
        {
            return await _db.Products.FindAsync(id);
        }

        // Helper to build JSON from the textarea input
        private static string? BuildAdditionalImagesJson(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var images = raw
                .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            return images.Count > 0
                ? JsonSerializer.Serialize(images)
                : null;
        }

        public async Task<Product?> GetBySlugAsync(string slug)
        {
            return await _db.Products
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .FirstOrDefaultAsync(p => p.Slug == slug);
        }

        public async Task<IReadOnlyList<Product>> GetRelatedProductsAsync(int productId, int maxItems = 12)
        {
            var product = await _db.Products
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return Array.Empty<Product>();

            // collect own category + parent category ids
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

            if (!categoryIds.Any())
                return Array.Empty<Product>();

            var query = _db.Products
                .AsNoTracking()
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .Where(p => p.Id != productId &&
                            p.ProductCategories.Any(pc => categoryIds.Contains(pc.CategoryId)));

            // score: how many shared categories → rating → rating count
            query = query
                .OrderByDescending(p =>
                    p.ProductCategories.Count(pc => categoryIds.Contains(pc.CategoryId)))
                .ThenByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.RatingCount ?? 0);

            return await query.Take(maxItems).ToListAsync();
        }

        public async Task<IReadOnlyList<Product>> GetOtherProductsAsync(int productId, int maxItems = 12)
        {
            // avoid duplicates with related section
            var related = await GetRelatedProductsAsync(productId, maxItems * 2);
            var excludedIds = related.Select(p => p.Id).Append(productId).ToHashSet();

            var query = _db.Products
                .AsNoTracking()
                .Where(p => !excludedIds.Contains(p.Id));

            // show "interesting" items – popular / well rated / latest
            query = query
                .OrderByDescending(p => p.ClickCount)      // if you have ClickCount
                .ThenByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.CreatedAt);

            return await query.Take(maxItems).ToListAsync();
        }
    }
}
