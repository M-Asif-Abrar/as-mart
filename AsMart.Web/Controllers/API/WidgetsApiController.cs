using AsMart.Web.Data;
using AsMart.Web.Models.Api;
using AsMart.Web.Models.DTOs;
using AsMart.Web.Models.Entities;
using AsMart.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Controllers.Api
{
    [ApiController]
    [Route("api/widgets")]
    [Produces("application/json")]
    [EnableRateLimiting("public-api")]
    public sealed class WidgetsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public WidgetsApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("home")]
        [ProducesResponseType(typeof(ApiResponse<PublicHomeWidgetsApiDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Home(
            [FromQuery] int productCount = 8,
            [FromQuery] int blogCount = 6,
            [FromQuery] int categoryCount = 12,
            [FromQuery] int collectionCount = 8,
            [FromQuery] int guideCount = 8,
            CancellationToken cancellationToken = default)
        {
            productCount = NormalizeCount(productCount, 8, 24);
            blogCount = NormalizeCount(blogCount, 6, 12);
            categoryCount = NormalizeCount(categoryCount, 12, 24);
            collectionCount = NormalizeCount(collectionCount, 8, 16);
            guideCount = NormalizeCount(guideCount, 8, 16);

            // Important: EF Core DbContext does not support concurrent queries.
            // These queries deliberately execute sequentially.
            var featured = await ProductQuery()
                .Where(product => product.IsFeatured)
                .OrderByDescending(product => product.ClickCount)
                .ThenByDescending(product => product.Rating ?? 0)
                .ThenByDescending(product => product.CreatedAt)
                .Take(productCount)
                .ToListAsync(cancellationToken);

            var deals = await ProductQuery()
                .Where(product => product.IsDealOfTheDay)
                .OrderByDescending(product => product.CreatedAt)
                .Take(productCount)
                .ToListAsync(cancellationToken);

            var popular = await ProductQuery()
                .OrderByDescending(product => product.ClickCount)
                .ThenByDescending(product => product.Rating ?? 0)
                .ThenByDescending(product => product.RatingCount ?? 0)
                .Take(productCount)
                .ToListAsync(cancellationToken);

            var latest = await ProductQuery()
                .OrderByDescending(product => product.CreatedAt)
                .Take(productCount)
                .ToListAsync(cancellationToken);

            var blogs = await _db.BlogPosts
                .AsNoTracking()
                .Where(blog => blog.IsPublished)
                .OrderByDescending(blog => blog.PublishedAt ?? blog.UpdatedAt ?? blog.CreatedAt)
                .Take(blogCount)
                .ToListAsync(cancellationToken);

            var categories = await _db.Categories
                .AsNoTracking()
                .OrderByDescending(category =>
                    category.ProductCategories.Count(item => item.Product.IsActive))
                .ThenBy(category => category.Name)
                .Take(categoryCount)
                .Select(category => new PublicCategoryApiDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Slug = category.Slug,
                    ProductCount = category.ProductCategories.Count(item => item.Product.IsActive)
                })
                .ToListAsync(cancellationToken);

            var collections = await _db.Collections
                .AsNoTracking()
                .OrderByDescending(collection =>
                    collection.CollectionProducts.Count(item => item.Product.IsActive))
                .ThenBy(collection => collection.Name)
                .Take(collectionCount)
                .Select(collection => new PublicCollectionApiDto
                {
                    Id = collection.Id,
                    Name = collection.Name,
                    Slug = collection.Slug,
                    ProductCount = collection.CollectionProducts.Count(item => item.Product.IsActive)
                })
                .ToListAsync(cancellationToken);

            var guides = await _db.SeoPages
                .AsNoTracking()
                .Where(page => page.Status == 1)
                .OrderByDescending(page => page.PublishedAt ?? page.UpdatedAt)
                .Take(guideCount)
                .ToListAsync(cancellationToken);

            var categoryIds = guides
                .Where(page => page.CategoryId.HasValue)
                .Select(page => page.CategoryId!.Value)
                .Distinct()
                .ToList();

            var categoryMap = categoryIds.Count == 0
                ? new Dictionary<int, Category>()
                : await _db.Categories
                    .AsNoTracking()
                    .Where(category => categoryIds.Contains(category.Id))
                    .ToDictionaryAsync(category => category.Id, cancellationToken);

            var data = new PublicHomeWidgetsApiDto
            {
                Products = new ProductHomeWidgetApiDto
                {
                    Featured = featured.Select(ToProductDto).ToList(),
                    Deals = deals.Select(ToProductDto).ToList(),
                    Popular = popular.Select(ToProductDto).ToList(),
                    Latest = latest.Select(ToProductDto).ToList()
                },
                LatestBlogs = blogs.Select(ToBlogDto).ToList(),
                Categories = categories,
                Collections = collections,
                LatestSeoGuides = guides
                    .Select(page => ToSeoGuideDto(page, categoryMap))
                    .ToList(),
                GeneratedAtUtc = DateTime.UtcNow
            };

            return Ok(ApiResponseFactory.Success(
                data,
                "Home widget data retrieved successfully.",
                new
                {
                    productCount,
                    blogCount,
                    categoryCount,
                    collectionCount,
                    guideCount
                }));
        }

        private IQueryable<Product> ProductQuery()
        {
            return _db.Products
                .AsNoTracking()
                .Include(product => product.ProductCategories)
                    .ThenInclude(item => item.Category)
                .Where(product => product.IsActive);
        }

        private PublicProductApiDto ToProductDto(Product product)
        {
            return new PublicProductApiDto
            {
                Id = product.Id,
                Title = product.Title,
                Slug = product.Slug,
                ShortDescription = product.ShortDescription,
                Brand = product.Brand,
                Price = product.Price,
                ListPrice = product.ListPrice,
                Currency = product.Currency,
                Rating = product.Rating,
                RatingCount = product.RatingCount,
                MainImageUrl = product.MainImageUrl,
                ProductUrl = BuildAbsoluteUrl($"/product/{product.Slug}")!,
                BuyUrl = BuildAbsoluteUrl($"/product/go/{product.Id}")!,
                IsFeatured = product.IsFeatured,
                IsDealOfTheDay = product.IsDealOfTheDay,
                ClickCount = product.ClickCount,
                Categories = product.ProductCategories
                    .Where(item => item.Category != null)
                    .Select(item => item.Category.Name)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList()
            };
        }

        private PublicBlogApiDto ToBlogDto(BlogPost blog)
        {
            return new PublicBlogApiDto
            {
                Id = blog.Id,
                Title = blog.Title,
                Slug = blog.Slug,
                MetaDescription = blog.MetaDescription,
                FeaturedImageUrl = BuildAbsoluteUrl(blog.FeaturedImageUrl),
                OgImageUrl = BuildAbsoluteUrl(
                    !string.IsNullOrWhiteSpace(blog.OgImageUrl)
                        ? blog.OgImageUrl
                        : blog.FeaturedImageUrl),
                BlogUrl = BuildAbsoluteUrl($"/blog/{blog.Slug}")!
            };
        }

        private PublicSeoPageApiDto ToSeoGuideDto(
            SeoPage page,
            IReadOnlyDictionary<int, Category> categoryMap)
        {
            Category? category = null;

            if (page.CategoryId.HasValue)
            {
                categoryMap.TryGetValue(page.CategoryId.Value, out category);
            }

            return new PublicSeoPageApiDto
            {
                Id = page.Id,
                Slug = page.Slug,
                Title = page.Title,
                MetaDescription = page.MetaDescription,
                H1 = page.H1,
                TemplateKey = page.TemplateKey,
                TargetKeyword = page.TargetKeyword,
                Brand = page.Brand,
                PriceMin = page.PriceMin,
                PriceMax = page.PriceMax,
                SortMode = page.SortMode,
                PublishedAt = page.PublishedAt,
                UpdatedAt = page.UpdatedAt,
                CategoryId = page.CategoryId,
                CategoryName = category?.Name,
                CategorySlug = category?.Slug,
                Url = BuildAbsoluteUrl($"/guides/{page.Slug}")
            };
        }

        private string? BuildAbsoluteUrl(string? pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl))
            {
                return null;
            }

            var value = pathOrUrl.Trim();

            if (Uri.TryCreate(value, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri.ToString();
            }

            return $"{Request.Scheme}://{Request.Host}/{value.TrimStart('/')}";
        }

        private static int NormalizeCount(int count, int defaultValue, int maximum)
        {
            return count <= 0 ? defaultValue : Math.Min(count, maximum);
        }
    }
}
