using AsMart.Web.Data;
using AsMart.Web.Models.DTOs;
using AsMart.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace AsMart.Web.Controllers.Api
{
    [ApiController]
    [Route("api/products")]
    [Produces("application/json")]
    [EnableRateLimiting("public-api")]
    public class ProductsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ProductsApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("featured")]
        public async Task<ActionResult<List<PublicProductApiDto>>> Featured([FromQuery] int count = 12)
        {
            count = NormalizeCount(count);

            var products = await BaseProductQuery()
                .Where(p => p.IsFeatured)
                .OrderByDescending(p => p.ClickCount)
                .ThenByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();

            return Ok(products.Select(ToPublicDto).ToList());
        }

        [HttpGet("latest")]
        public async Task<ActionResult<List<PublicProductApiDto>>> Latest([FromQuery] int count = 12)
        {
            count = NormalizeCount(count);

            var products = await BaseProductQuery()
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();

            return Ok(products.Select(ToPublicDto).ToList());
        }

        [HttpGet("popular")]
        public async Task<ActionResult<List<PublicProductApiDto>>> Popular([FromQuery] int count = 12)
        {
            count = NormalizeCount(count);

            var products = await BaseProductQuery()
                .OrderByDescending(p => p.ClickCount)
                .ThenByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.RatingCount ?? 0)
                .Take(count)
                .ToListAsync();

            return Ok(products.Select(ToPublicDto).ToList());
        }

        [HttpGet("deals")]
        public async Task<ActionResult<List<PublicProductApiDto>>> Deals([FromQuery] int count = 12)
        {
            count = NormalizeCount(count);

            var products = await BaseProductQuery()
                .Where(p => p.IsDealOfTheDay)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();

            return Ok(products.Select(ToPublicDto).ToList());
        }

        [HttpGet("top-rated")]
        public async Task<ActionResult<List<PublicProductApiDto>>> TopRated([FromQuery] int count = 12)
        {
            count = NormalizeCount(count);

            var products = await BaseProductQuery()
                .Where(p => p.Rating != null)
                .OrderByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.RatingCount ?? 0)
                .ThenByDescending(p => p.ClickCount)
                .Take(count)
                .ToListAsync();

            return Ok(products.Select(ToPublicDto).ToList());
        }

        [HttpGet("random")]
        public async Task<ActionResult<List<PublicProductApiDto>>> Random([FromQuery] int count = 6)
        {
            count = NormalizeCount(count);

            var products = await BaseProductQuery()
                .OrderBy(p => Guid.NewGuid())
                .Take(count)
                .ToListAsync();

            return Ok(products.Select(ToPublicDto).ToList());
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<PublicProductApiDto>>> Search(
            [FromQuery] string? q,
            [FromQuery] int count = 12)
        {
            count = NormalizeCount(count);

            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Search keyword is required.");

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
                .ToListAsync();

            return Ok(products.Select(ToPublicDto).ToList());
        }

        [HttpGet("category/{categorySlug}")]
        public async Task<ActionResult<List<PublicProductApiDto>>> ByCategory(
            string categorySlug,
            [FromQuery] int count = 12)
        {
            count = NormalizeCount(count);

            var products = await BaseProductQuery()
                .Where(p => p.ProductCategories.Any(pc => pc.Category.Slug == categorySlug))
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();

            return Ok(products.Select(ToPublicDto).ToList());
        }

        [HttpGet("brand/{brand}")]
        public async Task<ActionResult<List<PublicProductApiDto>>> ByBrand(
            string brand,
            [FromQuery] int count = 12)
        {
            count = NormalizeCount(count);

            var products = await BaseProductQuery()
                .Where(p => p.Brand != null && p.Brand == brand)
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();

            return Ok(products.Select(ToPublicDto).ToList());
        }

        [HttpGet("price-range")]
        public async Task<ActionResult<List<PublicProductApiDto>>> PriceRange(
            [FromQuery] decimal? min,
            [FromQuery] decimal? max,
            [FromQuery] int count = 12)
        {
            count = NormalizeCount(count);

            if (min == null && max == null)
                return BadRequest("Minimum or maximum price is required.");

            var query = BaseProductQuery();

            if (min != null)
                query = query.Where(p => p.Price >= min.Value);

            if (max != null)
                query = query.Where(p => p.Price <= max.Value);

            var products = await query
                .OrderBy(p => p.Price)
                .ThenByDescending(p => p.Rating ?? 0)
                .Take(count)
                .ToListAsync();

            return Ok(products.Select(ToPublicDto).ToList());
        }

        [HttpGet("related/{slug}")]
        public async Task<ActionResult<List<PublicProductApiDto>>> Related(
            string slug,
            [FromQuery] int count = 12)
        {
            count = NormalizeCount(count);

            var product = await _db.Products
                .AsNoTracking()
                .Include(p => p.ProductCategories)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

            if (product == null)
                return NotFound();

            var categoryIds = product.ProductCategories
                .Select(pc => pc.CategoryId)
                .Distinct()
                .ToList();

            if (!categoryIds.Any())
                return Ok(new List<PublicProductApiDto>());

            var relatedProducts = await BaseProductQuery()
                .Where(p =>
                    p.Id != product.Id &&
                    p.ProductCategories.Any(pc => categoryIds.Contains(pc.CategoryId)))
                .OrderByDescending(p => p.ProductCategories.Count(pc => categoryIds.Contains(pc.CategoryId)))
                .ThenByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.RatingCount ?? 0)
                .Take(count)
                .ToListAsync();

            return Ok(relatedProducts.Select(ToPublicDto).ToList());
        }

        [HttpGet("collection/{collectionSlug}")]
        public async Task<ActionResult<List<PublicProductApiDto>>> ByCollection(
            string collectionSlug,
            [FromQuery] int count = 12)
        {
            count = NormalizeCount(count);

            var products = await BaseProductQuery()
                .Where(p => p.CollectionProducts.Any(cp => cp.Collection.Slug == collectionSlug))
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.Rating ?? 0)
                .ThenByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();

            return Ok(products.Select(ToPublicDto).ToList());
        }

        [HttpGet("home-widget")]
        public async Task<ActionResult<ProductHomeWidgetApiDto>> HomeWidget([FromQuery] int count = 8)
        {
            count = NormalizeCount(count);

            var featured = await BaseProductQuery()
                .Where(p => p.IsFeatured)
                .OrderByDescending(p => p.ClickCount)
                .ThenByDescending(p => p.Rating ?? 0)
                .Take(count)
                .ToListAsync();

            var deals = await BaseProductQuery()
                .Where(p => p.IsDealOfTheDay)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();

            var popular = await BaseProductQuery()
                .OrderByDescending(p => p.ClickCount)
                .ThenByDescending(p => p.Rating ?? 0)
                .Take(count)
                .ToListAsync();

            var latest = await BaseProductQuery()
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();

            return Ok(new ProductHomeWidgetApiDto
            {
                Featured = featured.Select(ToPublicDto).ToList(),
                Deals = deals.Select(ToPublicDto).ToList(),
                Popular = popular.Select(ToPublicDto).ToList(),
                Latest = latest.Select(ToPublicDto).ToList()
            });
        }

        [HttpGet("promote/{slug}")]
        public async Task<ActionResult<PublicProductApiDto>> Promote(string slug)
        {
            var product = await BaseProductQuery()
                .FirstOrDefaultAsync(p => p.Slug == slug);

            if (product == null)
                return NotFound();

            return Ok(ToPublicDto(product));
        }

        [HttpGet("{slug}")]
        public async Task<ActionResult<PublicProductApiDto>> BySlug(string slug)
        {
            var product = await BaseProductQuery()
                .FirstOrDefaultAsync(p => p.Slug == slug);

            if (product == null)
                return NotFound();

            return Ok(ToPublicDto(product));
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

        private PublicProductApiDto ToPublicDto(Product product)
        {
            var productUrl = Url.Action(
                action: "Details",
                controller: "Product",
                values: new { slug = product.Slug },
                protocol: Request.Scheme
            ) ?? $"/product/{product.Slug}";

            var buyUrl = Url.Action(
                action: "Go",
                controller: "Product",
                values: new { id = product.Id },
                protocol: Request.Scheme
            ) ?? $"/product/go/{product.Id}";

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
                return 12;

            if (count > 50)
                return 50;

            return count;
        }
    }
}