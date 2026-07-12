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
    [Route("api/products")]
    [Produces("application/json")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public sealed class ProductsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ProductsApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("featured")]
        [ProducesResponseType(typeof(ApiResponse<List<PublicProductApiDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Featured(
            [FromQuery] int count = 12,
            CancellationToken cancellationToken = default)
        {
            count = NormalizeCount(count);

            var products = await BaseProductQuery()
                .Where(p => p.IsFeatured)
                .OrderByDescending(p => p.ClickCount)
                .ThenByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);

            return ProductsListResponse(
                products,
                count,
                "Featured products retrieved successfully.");
        }

        [HttpGet("latest")]
        [ProducesResponseType(typeof(ApiResponse<List<PublicProductApiDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Latest(
            [FromQuery] int count = 12,
            CancellationToken cancellationToken = default)
        {
            count = NormalizeCount(count);

            var products = await BaseProductQuery()
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);

            return ProductsListResponse(
                products,
                count,
                "Latest products retrieved successfully.");
        }

        [HttpGet("popular")]
        [ProducesResponseType(typeof(ApiResponse<List<PublicProductApiDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Popular(
            [FromQuery] int count = 12,
            CancellationToken cancellationToken = default)
        {
            count = NormalizeCount(count);

            var products = await BaseProductQuery()
                .OrderByDescending(p => p.ClickCount)
                .ThenByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.RatingCount ?? 0)
                .Take(count)
                .ToListAsync(cancellationToken);

            return ProductsListResponse(
                products,
                count,
                "Popular products retrieved successfully.");
        }

        [HttpGet("deals")]
        [ProducesResponseType(typeof(ApiResponse<List<PublicProductApiDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Deals(
            [FromQuery] int count = 12,
            CancellationToken cancellationToken = default)
        {
            count = NormalizeCount(count);

            var products = await BaseProductQuery()
                .Where(p => p.IsDealOfTheDay)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);

            return ProductsListResponse(
                products,
                count,
                "Deal products retrieved successfully.");
        }

        [HttpGet("top-rated")]
        [ProducesResponseType(typeof(ApiResponse<List<PublicProductApiDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TopRated(
            [FromQuery] int count = 12,
            CancellationToken cancellationToken = default)
        {
            count = NormalizeCount(count);

            var products = await BaseProductQuery()
                .Where(p => p.Rating != null)
                .OrderByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.RatingCount ?? 0)
                .ThenByDescending(p => p.ClickCount)
                .Take(count)
                .ToListAsync(cancellationToken);

            return ProductsListResponse(
                products,
                count,
                "Top-rated products retrieved successfully.");
        }

        [HttpGet("random")]
        [ProducesResponseType(typeof(ApiResponse<List<PublicProductApiDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Random(
            [FromQuery] int count = 6,
            CancellationToken cancellationToken = default)
        {
            count = NormalizeCount(count);

            var products = await BaseProductQuery()
                .OrderBy(_ => Guid.NewGuid())
                .Take(count)
                .ToListAsync(cancellationToken);

            return ProductsListResponse(
                products,
                count,
                "Random products retrieved successfully.");
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<PublicProductApiDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search(
            [FromQuery] string? q,
            [FromQuery] int count = 12,
            CancellationToken cancellationToken = default)
        {
            count = NormalizeCount(count);

            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(ApiResponseFactory.Error(
                    ApiErrorCodes.ValidationFailed,
                    "A search keyword is required.",
                    HttpContext.TraceIdentifier,
                    new Dictionary<string, string[]>
                    {
                        ["q"] = ["The q query parameter is required."]
                    }));
            }

            q = q.Trim();

            var products = await BaseProductQuery()
                .Where(p =>
                    p.Title.Contains(q) ||
                    (p.Brand != null && p.Brand.Contains(q)) ||
                    (p.ShortDescription != null && p.ShortDescription.Contains(q)))
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.ClickCount)
                .ThenByDescending(p => p.Rating ?? 0)
                .Take(count)
                .ToListAsync(cancellationToken);

            return ProductsListResponse(
                products,
                count,
                "Product search completed successfully.",
                new { query = q });
        }

        [HttpGet("category/{categorySlug}")]
        [ProducesResponseType(typeof(ApiResponse<List<PublicProductApiDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ByCategory(
            string categorySlug,
            [FromQuery] int count = 12,
            CancellationToken cancellationToken = default)
        {
            count = NormalizeCount(count);
            categorySlug = categorySlug.Trim();

            var categoryExists = await _db.Categories
                .AsNoTracking()
                .AnyAsync(c => c.Slug == categorySlug, cancellationToken);

            if (!categoryExists)
            {
                return NotFound(ApiResponseFactory.Error(
                    ApiErrorCodes.CategoryNotFound,
                    $"No category was found for slug '{categorySlug}'.",
                    HttpContext.TraceIdentifier));
            }

            var products = await BaseProductQuery()
                .Where(p => p.ProductCategories.Any(
                    pc => pc.Category.Slug == categorySlug))
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);

            return ProductsListResponse(
                products,
                count,
                "Category products retrieved successfully.",
                new { categorySlug });
        }

        [HttpGet("brand/{brand}")]
        [ProducesResponseType(typeof(ApiResponse<List<PublicProductApiDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ByBrand(
            string brand,
            [FromQuery] int count = 12,
            CancellationToken cancellationToken = default)
        {
            count = NormalizeCount(count);
            brand = brand.Trim();

            var products = await BaseProductQuery()
                .Where(p => p.Brand != null && p.Brand == brand)
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);

            return ProductsListResponse(
                products,
                count,
                "Brand products retrieved successfully.",
                new { brand });
        }

        [HttpGet("price-range")]
        [ProducesResponseType(typeof(ApiResponse<List<PublicProductApiDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PriceRange(
            [FromQuery] decimal? min,
            [FromQuery] decimal? max,
            [FromQuery] int count = 12,
            CancellationToken cancellationToken = default)
        {
            count = NormalizeCount(count);

            if (!min.HasValue && !max.HasValue)
            {
                return BadRequest(ApiResponseFactory.Error(
                    ApiErrorCodes.ValidationFailed,
                    "At least one price boundary is required.",
                    HttpContext.TraceIdentifier,
                    new Dictionary<string, string[]>
                    {
                        ["priceRange"] = ["Provide min, max, or both."]
                    }));
            }

            if (min.HasValue && min.Value < 0)
            {
                return BadRequest(ApiResponseFactory.Error(
                    ApiErrorCodes.ValidationFailed,
                    "The minimum price cannot be negative.",
                    HttpContext.TraceIdentifier,
                    new Dictionary<string, string[]>
                    {
                        ["min"] = ["The minimum price must be zero or greater."]
                    }));
            }

            if (max.HasValue && max.Value < 0)
            {
                return BadRequest(ApiResponseFactory.Error(
                    ApiErrorCodes.ValidationFailed,
                    "The maximum price cannot be negative.",
                    HttpContext.TraceIdentifier,
                    new Dictionary<string, string[]>
                    {
                        ["max"] = ["The maximum price must be zero or greater."]
                    }));
            }

            if (min.HasValue && max.HasValue && min.Value > max.Value)
            {
                return BadRequest(ApiResponseFactory.Error(
                    ApiErrorCodes.ValidationFailed,
                    "The minimum price cannot exceed the maximum price.",
                    HttpContext.TraceIdentifier,
                    new Dictionary<string, string[]>
                    {
                        ["min"] = ["The minimum price must be less than or equal to the maximum price."]
                    }));
            }

            var query = BaseProductQuery();

            if (min.HasValue)
            {
                query = query.Where(p => p.Price >= min.Value);
            }

            if (max.HasValue)
            {
                query = query.Where(p => p.Price <= max.Value);
            }

            var products = await query
                .OrderBy(p => p.Price)
                .ThenByDescending(p => p.Rating ?? 0)
                .Take(count)
                .ToListAsync(cancellationToken);

            return ProductsListResponse(
                products,
                count,
                "Products in the requested price range were retrieved successfully.",
                new { min, max });
        }

        [HttpGet("related/{slug}")]
        [ProducesResponseType(typeof(ApiResponse<List<PublicProductApiDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Related(
            string slug,
            [FromQuery] int count = 12,
            CancellationToken cancellationToken = default)
        {
            count = NormalizeCount(count);
            slug = slug.Trim();

            var product = await _db.Products
                .AsNoTracking()
                .Include(p => p.ProductCategories)
                .FirstOrDefaultAsync(
                    p => p.Slug == slug && p.IsActive,
                    cancellationToken);

            if (product is null)
            {
                return NotFound(ApiResponseFactory.Error(
                    ApiErrorCodes.ProductNotFound,
                    $"No active product was found for slug '{slug}'.",
                    HttpContext.TraceIdentifier));
            }

            var categoryIds = product.ProductCategories
                .Select(pc => pc.CategoryId)
                .Distinct()
                .ToList();

            if (categoryIds.Count == 0)
            {
                return Ok(ApiResponseFactory.Success(
                    new List<PublicProductApiDto>(),
                    "No related products were found.",
                    new
                    {
                        requestedCount = count,
                        returnedCount = 0,
                        sourceProductSlug = slug
                    }));
            }

            var relatedProducts = await BaseProductQuery()
                .Where(p =>
                    p.Id != product.Id &&
                    p.ProductCategories.Any(
                        pc => categoryIds.Contains(pc.CategoryId)))
                .OrderByDescending(p =>
                    p.ProductCategories.Count(
                        pc => categoryIds.Contains(pc.CategoryId)))
                .ThenByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.RatingCount ?? 0)
                .Take(count)
                .ToListAsync(cancellationToken);

            return ProductsListResponse(
                relatedProducts,
                count,
                "Related products retrieved successfully.",
                new { sourceProductSlug = slug });
        }

        [HttpGet("collection/{collectionSlug}")]
        [ProducesResponseType(typeof(ApiResponse<List<PublicProductApiDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ByCollection(
            string collectionSlug,
            [FromQuery] int count = 12,
            CancellationToken cancellationToken = default)
        {
            count = NormalizeCount(count);
            collectionSlug = collectionSlug.Trim();

            var products = await BaseProductQuery()
                .Where(p => p.CollectionProducts.Any(
                    cp => cp.Collection.Slug == collectionSlug))
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);

            return ProductsListResponse(
                products,
                count,
                "Collection products retrieved successfully.",
                new { collectionSlug });
        }

        [HttpGet("home-widget")]
        [ProducesResponseType(typeof(ApiResponse<ProductHomeWidgetApiDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> HomeWidget(
            [FromQuery] int count = 8,
            CancellationToken cancellationToken = default)
        {
            count = NormalizeCount(count);

            var featured = await BaseProductQuery()
                .Where(p => p.IsFeatured)
                .OrderByDescending(p => p.ClickCount)
                .ThenByDescending(p => p.Rating ?? 0)
                .Take(count)
                .ToListAsync(cancellationToken);

            var deals = await BaseProductQuery()
                .Where(p => p.IsDealOfTheDay)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);

            var popular = await BaseProductQuery()
                .OrderByDescending(p => p.ClickCount)
                .ThenByDescending(p => p.Rating ?? 0)
                .Take(count)
                .ToListAsync(cancellationToken);

            var latest = await BaseProductQuery()
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);

            var data = new ProductHomeWidgetApiDto
            {
                Featured = featured.Select(ToPublicDto).ToList(),
                Deals = deals.Select(ToPublicDto).ToList(),
                Popular = popular.Select(ToPublicDto).ToList(),
                Latest = latest.Select(ToPublicDto).ToList()
            };

            return Ok(ApiResponseFactory.Success(
                data,
                "Product home-widget data retrieved successfully.",
                new { requestedCountPerSection = count }));
        }

        [HttpGet("promote/{slug}")]
        [ProducesResponseType(typeof(ApiResponse<PublicProductApiDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Promote(
            string slug,
            CancellationToken cancellationToken = default)
        {
            slug = slug.Trim();

            var product = await BaseProductQuery()
                .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);

            if (product is null)
            {
                return ProductNotFound(slug);
            }

            return Ok(ApiResponseFactory.Success(
                ToPublicDto(product),
                "Product promotion data retrieved successfully."));
        }

        [HttpGet("{slug}")]
        [ProducesResponseType(typeof(ApiResponse<PublicProductApiDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> BySlug(
            string slug,
            CancellationToken cancellationToken = default)
        {
            slug = slug.Trim();

            var product = await BaseProductQuery()
                .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);

            if (product is null)
            {
                return ProductNotFound(slug);
            }

            return Ok(ApiResponseFactory.Success(
                ToPublicDto(product),
                "Product retrieved successfully."));
        }

        private IQueryable<Product> BaseProductQuery()
        {
            return _db.Products
                .AsNoTracking()
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .Include(p => p.CollectionProducts)
                    .ThenInclude(cp => cp.Collection)
                .Where(p => p.IsActive);
        }

        private IActionResult ProductsListResponse(
            IEnumerable<Product> products,
            int requestedCount,
            string message,
            object? additionalMeta = null)
        {
            var data = products
                .Select(ToPublicDto)
                .ToList();

            var meta = new
            {
                requestedCount,
                returnedCount = data.Count,
                context = additionalMeta
            };

            return Ok(ApiResponseFactory.Success(data, message, meta));
        }

        private IActionResult ProductNotFound(string slug)
        {
            return NotFound(ApiResponseFactory.Error(
                ApiErrorCodes.ProductNotFound,
                $"No active product was found for slug '{slug}'.",
                HttpContext.TraceIdentifier));
        }

        private PublicProductApiDto ToPublicDto(Product product)
        {
            var productUrl = Url.Action(
                action: "Details",
                controller: "Product",
                values: new { slug = product.Slug },
                protocol: Request.Scheme)
                ?? $"/product/{product.Slug}";

            var buyUrl = Url.Action(
                action: "Go",
                controller: "Product",
                values: new { id = product.Id },
                protocol: Request.Scheme)
                ?? $"/product/go/{product.Id}";

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
                ProductUrl = productUrl,
                BuyUrl = buyUrl,
                IsFeatured = product.IsFeatured,
                IsDealOfTheDay = product.IsDealOfTheDay,
                ClickCount = product.ClickCount,
                Categories = product.ProductCategories
                    .Where(pc => pc.Category != null)
                    .Select(pc => pc.Category.Name)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList()
            };
        }

        private static int NormalizeCount(int count)
        {
            if (count <= 0)
            {
                return 12;
            }

            return Math.Min(count, 50);
        }
    }
}