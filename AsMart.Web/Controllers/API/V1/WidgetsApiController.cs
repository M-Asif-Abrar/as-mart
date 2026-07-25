using Asp.Versioning;
using AsMart.Web.Data;
using AsMart.Web.Models.Api;
using AsMart.Web.Models.DTOs;
using AsMart.Web.Models.Entities;
using AsMart.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Controllers.Api.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/widgets")]
    [Produces("application/json")]
    [EnableRateLimiting("public-api")]
    public sealed class WidgetsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;

        public WidgetsApiController(
            ApplicationDbContext db,
            IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        /// <summary>
        /// Returns the combined home-page widget data used by external clients.
        /// </summary>
        /// <param name="productCount">
        /// Maximum number of products returned in each product section.
        /// </param>
        /// <param name="blogCount">
        /// Maximum number of latest published blog posts returned.
        /// </param>
        /// <param name="categoryCount">
        /// Maximum number of product categories returned.
        /// </param>
        /// <param name="collectionCount">
        /// Maximum number of product collections returned.
        /// </param>
        /// <param name="guideCount">
        /// Maximum number of published SEO guides returned.
        /// </param>
        /// <param name="cancellationToken">
        /// Request cancellation token.
        /// </param>
        /// <returns>
        /// A standardized API response containing products, blogs, categories,
        /// collections, and SEO guides.
        /// </returns>
        [HttpGet("home")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType(
            typeof(ApiResponse<PublicHomeWidgetsApiDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Home(
            [FromQuery] int productCount = 8,
            [FromQuery] int blogCount = 6,
            [FromQuery] int categoryCount = 12,
            [FromQuery] int collectionCount = 8,
            [FromQuery] int guideCount = 8,
            CancellationToken cancellationToken = default)
        {
            productCount = NormalizeCount(
                productCount,
                defaultValue: 8,
                maximum: 24);

            blogCount = NormalizeCount(
                blogCount,
                defaultValue: 6,
                maximum: 12);

            categoryCount = NormalizeCount(
                categoryCount,
                defaultValue: 12,
                maximum: 24);

            collectionCount = NormalizeCount(
                collectionCount,
                defaultValue: 8,
                maximum: 16);

            guideCount = NormalizeCount(
                guideCount,
                defaultValue: 8,
                maximum: 16);

            /*
             * ApplicationDbContext is not thread-safe.
             * Run these EF Core operations sequentially rather than using
             * Task.WhenAll against the same DbContext instance.
             */

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
                .OrderByDescending(
                    blog => blog.PublishedAt
                            ?? blog.UpdatedAt
                            ?? blog.CreatedAt)
                .Take(blogCount)
                .ToListAsync(cancellationToken);

            var categories = await _db.Categories
                .AsNoTracking()
                .OrderByDescending(category =>
                    category.ProductCategories.Count(
                        item => item.Product.IsActive))
                .ThenBy(category => category.Name)
                .Take(categoryCount)
                .Select(category => new PublicCategoryApiDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Slug = category.Slug,
                    ProductCount = category.ProductCategories.Count(
                        item => item.Product.IsActive)
                })
                .ToListAsync(cancellationToken);

            var collections = await _db.Collections
                .AsNoTracking()
                .OrderByDescending(collection =>
                    collection.CollectionProducts.Count(
                        item => item.Product.IsActive))
                .ThenBy(collection => collection.Name)
                .Take(collectionCount)
                .Select(collection => new PublicCollectionApiDto
                {
                    Id = collection.Id,
                    Name = collection.Name,
                    Slug = collection.Slug,
                    ProductCount = collection.CollectionProducts.Count(
                        item => item.Product.IsActive)
                })
                .ToListAsync(cancellationToken);

            var guides = await _db.SeoPages
                .AsNoTracking()
                .Where(page => page.Status == 1)
                .OrderByDescending(
                    page => page.PublishedAt ?? page.UpdatedAt)
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
                    .ToDictionaryAsync(
                        category => category.Id,
                        cancellationToken);

            var data = new PublicHomeWidgetsApiDto
            {
                Products = new ProductHomeWidgetApiDto
                {
                    Featured = featured
                        .Select(ToProductDto)
                        .ToList(),

                    Deals = deals
                        .Select(ToProductDto)
                        .ToList(),

                    Popular = popular
                        .Select(ToProductDto)
                        .ToList(),

                    Latest = latest
                        .Select(ToProductDto)
                        .ToList()
                },

                LatestBlogs = blogs
                    .Select(ToBlogDto)
                    .ToList(),

                Categories = categories,

                Collections = collections,

                LatestSeoGuides = guides
                    .Select(page => ToSeoGuideDto(page, categoryMap))
                    .ToList(),

                GeneratedAtUtc = DateTime.UtcNow
            };

            return Ok(
                ApiResponseFactory.Success(
                    data,
                    "Home widget data retrieved successfully.",
                    new
                    {
                        apiVersion = "1.0",
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
                MainImageUrl = BuildAbsoluteUrl(product.MainImageUrl),

                ProductUrl = BuildAbsoluteUrl(
                    $"/product/{product.Slug}")!,

                BuyUrl = BuildAbsoluteUrl(
                    $"/product/go/{product.Id}")!,

                IsFeatured = product.IsFeatured,
                IsDealOfTheDay = product.IsDealOfTheDay,
                ClickCount = product.ClickCount,

                Categories = product.ProductCategories
                    .Where(item => item.Category != null)
                    .Select(item => item.Category!.Name)
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

                FeaturedImageUrl = BuildAbsoluteUrl(
                    blog.FeaturedImageUrl),

                OgImageUrl = BuildAbsoluteUrl(
                    !string.IsNullOrWhiteSpace(blog.OgImageUrl)
                        ? blog.OgImageUrl
                        : blog.FeaturedImageUrl),

                BlogUrl = BuildAbsoluteUrl(
                    $"/blog/{blog.Slug}")!
            };
        }

        private PublicSeoPageApiDto ToSeoGuideDto(
            SeoPage page,
            IReadOnlyDictionary<int, Category> categoryMap)
        {
            Category? category = null;

            if (page.CategoryId.HasValue)
            {
                categoryMap.TryGetValue(
                    page.CategoryId.Value,
                    out category);
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

                Url = BuildAbsoluteUrl(
                    $"/guides/{page.Slug}")
            };
        }

        private string? BuildAbsoluteUrl(string? pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl))
            {
                return null;
            }

            var value = pathOrUrl.Trim();

            if (Uri.TryCreate(
                    value,
                    UriKind.Absolute,
                    out var absoluteUri))
            {
                return absoluteUri.ToString();
            }

            var configuredBaseUrl = _configuration["PublicSite:BaseUrl"];

            var baseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
                ? $"{Request.Scheme}://{Request.Host}"
                : configuredBaseUrl.Trim().TrimEnd('/');

            return $"{baseUrl}/{value.TrimStart('/')}";
        }

        private static int NormalizeCount(
            int count,
            int defaultValue,
            int maximum)
        {
            return count <= 0
                ? defaultValue
                : Math.Min(count, maximum);
        }
    }
}