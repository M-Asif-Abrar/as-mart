using Asp.Versioning;
using AsMart.Web.Data;
using AsMart.Web.Models.DTOs;
using AsMart.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace AsMart.Web.Controllers.API.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/seopages")]
    [Produces("application/json")]
    [EnableRateLimiting("public-api")]
    public class SeoPagesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;

        public SeoPagesApiController(
            ApplicationDbContext db,
            IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<PublicSeoPageListApiDto>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            page = Math.Max(page, 1);
            pageSize = NormalizePageSize(pageSize);

            var query = PublishedQuery();

            var total = await query.CountAsync();

            var pages = await query
                .OrderByDescending(x => x.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categoryMap = await GetCategoryMapAsync();

            return Ok(new PublicSeoPageListApiDto
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Items = pages.Select(x => ToListDto(x, categoryMap)).ToList()
            });
        }

        [HttpGet("latest")]
        public async Task<ActionResult<List<PublicSeoPageApiDto>>> Latest([FromQuery] int count = 20)
        {
            count = NormalizeCount(count);

            var pages = await PublishedQuery()
                .OrderByDescending(x => x.PublishedAt ?? x.UpdatedAt)
                .Take(count)
                .ToListAsync();

            var categoryMap = await GetCategoryMapAsync();

            return Ok(pages.Select(x => ToListDto(x, categoryMap)).ToList());
        }

        [HttpGet("random")]
        public async Task<ActionResult<List<PublicSeoPageApiDto>>> Random([FromQuery] int count = 10)
        {
            count = NormalizeCount(count);

            var pages = await PublishedQuery()
                .OrderBy(x => Guid.NewGuid())
                .Take(count)
                .ToListAsync();

            var categoryMap = await GetCategoryMapAsync();

            return Ok(pages.Select(x => ToListDto(x, categoryMap)).ToList());
        }

        [HttpGet("search")]
        public async Task<ActionResult<PublicSeoPageListApiDto>> Search(
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Search keyword is required.");

            q = q.Trim();
            page = Math.Max(page, 1);
            pageSize = NormalizePageSize(pageSize);

            var query = PublishedQuery()
                .Where(x =>
                    x.Title.Contains(q) ||
                    x.Slug.Contains(q) ||
                    x.TargetKeyword.Contains(q) ||
                    x.H1 != null && x.H1.Contains(q) ||
                    x.MetaDescription != null && x.MetaDescription.Contains(q));

            var total = await query.CountAsync();

            var pages = await query
                .OrderByDescending(x => x.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categoryMap = await GetCategoryMapAsync();

            return Ok(new PublicSeoPageListApiDto
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Items = pages.Select(x => ToListDto(x, categoryMap)).ToList()
            });
        }

        [HttpGet("filter")]
        public async Task<ActionResult<PublicSeoPageListApiDto>> Filter(
            [FromQuery] string? q,
            [FromQuery] string? category,
            [FromQuery] string? brand,
            [FromQuery] string? template,
            [FromQuery] string? sortMode,
            [FromQuery] decimal? priceMin,
            [FromQuery] decimal? priceMax,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            page = Math.Max(page, 1);
            pageSize = NormalizePageSize(pageSize);

            var query = PublishedQuery();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();

                query = query.Where(x =>
                    x.Title.Contains(q) ||
                    x.Slug.Contains(q) ||
                    x.TargetKeyword.Contains(q) ||
                    x.H1 != null && x.H1.Contains(q) ||
                    x.MetaDescription != null && x.MetaDescription.Contains(q));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                category = category.Trim();

                var categoryIds = await _db.Categories
                    .AsNoTracking()
                    .Where(c => c.Slug == category)
                    .Select(c => c.Id)
                    .ToListAsync();

                query = query.Where(x => x.CategoryId.HasValue && categoryIds.Contains(x.CategoryId.Value));
            }

            if (!string.IsNullOrWhiteSpace(brand))
            {
                brand = brand.Trim();
                query = query.Where(x => x.Brand != null && x.Brand.Contains(brand));
            }

            if (!string.IsNullOrWhiteSpace(template))
            {
                template = template.Trim();
                query = query.Where(x => x.TemplateKey == template);
            }

            sortMode = string.IsNullOrWhiteSpace(sortMode)
                ? "latest"
                : sortMode.Trim().ToLowerInvariant();

            if (priceMin.HasValue)
                query = query.Where(x => x.PriceMin == null || x.PriceMin >= priceMin.Value);

            if (priceMax.HasValue)
                query = query.Where(x => x.PriceMax == null || x.PriceMax <= priceMax.Value);

            var total = await query.CountAsync();

            var orderedQuery = sortMode switch
            {
                "oldest" => query
                    .OrderBy(x => x.PublishedAt ?? x.UpdatedAt),

                "title" => query
                    .OrderBy(x => x.Title),

                "price-low-high" => query
                    .OrderBy(x => x.PriceMin ?? x.PriceMax ?? decimal.MaxValue)
                    .ThenByDescending(x => x.UpdatedAt),

                "price-high-low" => query
                    .OrderByDescending(x => x.PriceMax ?? x.PriceMin ?? 0m)
                    .ThenByDescending(x => x.UpdatedAt),

                "rank" => query
                    .OrderByDescending(x => x.SortMode == "rank")
                    .ThenByDescending(x => x.PublishedAt ?? x.UpdatedAt),

                _ => query
                    .OrderByDescending(x => x.PublishedAt ?? x.UpdatedAt)
            };

            var pages = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categoryMap = await GetCategoryMapAsync();

            return Ok(new PublicSeoPageListApiDto
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Items = pages.Select(x => ToListDto(x, categoryMap)).ToList()
            });
        }

        [HttpGet("category/{categorySlug}")]
        public async Task<ActionResult<List<PublicSeoPageApiDto>>> ByCategory(
            string categorySlug,
            [FromQuery] int count = 20)
        {
            count = NormalizeCount(count);

            var categoryIds = await _db.Categories
                .AsNoTracking()
                .Where(c => c.Slug == categorySlug)
                .Select(c => c.Id)
                .ToListAsync();

            if (!categoryIds.Any())
                return Ok(new List<PublicSeoPageApiDto>());

            var pages = await PublishedQuery()
                .Where(x => x.CategoryId.HasValue && categoryIds.Contains(x.CategoryId.Value))
                .OrderByDescending(x => x.UpdatedAt)
                .Take(count)
                .ToListAsync();

            var categoryMap = await GetCategoryMapAsync();

            return Ok(pages.Select(x => ToListDto(x, categoryMap)).ToList());
        }

        [HttpGet("categories")]
        public async Task<ActionResult<List<PublicSeoPageCategoryApiDto>>> Categories()
        {
            var categories = await PublishedQuery()
                .Where(x => x.CategoryId.HasValue)
                .Join(
                    _db.Categories.AsNoTracking(),
                    seo => seo.CategoryId!.Value,
                    cat => cat.Id,
                    (seo, cat) => new { seo, cat }
                )
                .GroupBy(x => new
                {
                    CategoryId = x.cat.Id,
                    CategoryName = x.cat.Name,
                    CategorySlug = x.cat.Slug
                })
                .Select(g => new PublicSeoPageCategoryApiDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName ?? "",
                    CategorySlug = g.Key.CategorySlug ?? "",
                    PageCount = g.Count()
                })
                .OrderByDescending(x => x.PageCount)
                .ThenBy(x => x.CategoryName)
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("related/{slug}")]
        public async Task<ActionResult<List<PublicSeoPageApiDto>>> Related(
            string slug,
            [FromQuery] int count = 10)
        {
            count = NormalizeCount(count);

            var current = await PublishedQuery()
                .FirstOrDefaultAsync(x => x.Slug == slug);

            if (current == null)
                return NotFound();

            var query = PublishedQuery()
                .Where(x => x.Id != current.Id);

            if (current.CategoryId.HasValue)
                query = query.Where(x => x.CategoryId == current.CategoryId);

            var pages = await query
                .OrderByDescending(x => x.UpdatedAt)
                .Take(count)
                .ToListAsync();

            var categoryMap = await GetCategoryMapAsync();

            return Ok(pages.Select(x => ToListDto(x, categoryMap)).ToList());
        }

        [HttpGet("home-widget")]
        public async Task<ActionResult<PublicSeoPageHomeWidgetApiDto>> HomeWidget([FromQuery] int count = 8)
        {
            count = NormalizeCount(count);

            var latest = await PublishedQuery()
                .OrderByDescending(x => x.PublishedAt ?? x.UpdatedAt)
                .Take(count)
                .ToListAsync();

            var random = await PublishedQuery()
                .OrderBy(x => Guid.NewGuid())
                .Take(count)
                .ToListAsync();

            var categories = await PublishedQuery()
                .Where(x => x.CategoryId.HasValue)
                .Join(
                    _db.Categories.AsNoTracking(),
                    seo => seo.CategoryId!.Value,
                    cat => cat.Id,
                    (seo, cat) => new { seo, cat }
                )
                .GroupBy(x => new
                {
                    CategoryId = x.cat.Id,
                    CategoryName = x.cat.Name,
                    CategorySlug = x.cat.Slug
                })
                .Select(g => new PublicSeoPageCategoryApiDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName ?? "",
                    CategorySlug = g.Key.CategorySlug ?? "",
                    PageCount = g.Count()
                })
                .OrderByDescending(x => x.PageCount)
                .Take(count)
                .ToListAsync();

            var categoryMap = await GetCategoryMapAsync();

            return Ok(new PublicSeoPageHomeWidgetApiDto
            {
                Latest = latest.Select(x => ToListDto(x, categoryMap)).ToList(),
                Random = random.Select(x => ToListDto(x, categoryMap)).ToList(),
                Categories = categories
            });
        }

        [HttpGet("stats")]
        public async Task<ActionResult<PublicSeoPageStatsApiDto>> Stats()
        {
            var total = await _db.SeoPages.CountAsync();

            var published = await _db.SeoPages
                .CountAsync(x => x.Status == 1);

            var categories = await _db.SeoPages
                .Where(x => x.Status == 1 && x.CategoryId.HasValue)
                .Select(x => x.CategoryId)
                .Distinct()
                .CountAsync();

            var lastUpdated = await _db.SeoPages
                .Where(x => x.Status == 1)
                .OrderByDescending(x => x.UpdatedAt)
                .Select(x => (DateTime?)x.UpdatedAt)
                .FirstOrDefaultAsync();

            return Ok(new PublicSeoPageStatsApiDto
            {
                TotalPages = total,
                PublishedPages = published,
                Categories = categories,
                LastUpdated = lastUpdated
            });
        }

        [HttpGet("sitemap")]
        public async Task<ActionResult<List<PublicSeoPageSitemapApiDto>>> Sitemap()
        {
            var pages = await PublishedQuery()
                .OrderByDescending(x => x.UpdatedAt)
                .Select(x => new PublicSeoPageSitemapApiDto
                {
                    Slug = x.Slug,
                    Url = BuildGuideUrl(x.Slug),
                    UpdatedAt = x.UpdatedAt,
                    PublishedAt = x.PublishedAt
                })
                .ToListAsync();

            return Ok(pages);
        }

        [HttpGet("templates")]
        public async Task<ActionResult<List<string>>> Templates()
        {
            var templates = await PublishedQuery()
                .Where(x => x.TemplateKey != "")
                .Select(x => x.TemplateKey)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return Ok(templates);
        }

        [HttpGet("template/{template}")]
        public async Task<ActionResult<List<PublicSeoPageApiDto>>> ByTemplate(
            string template,
            [FromQuery] int count = 20)
        {
            count = NormalizeCount(count);

            var pages = await PublishedQuery()
                .Where(x => x.TemplateKey == template)
                .OrderByDescending(x => x.UpdatedAt)
                .Take(count)
                .ToListAsync();

            var categoryMap = await GetCategoryMapAsync();

            return Ok(pages.Select(x => ToListDto(x, categoryMap)).ToList());
        }

        [HttpGet("brands")]
        public async Task<ActionResult<List<string>>> Brands()
        {
            var brands = await PublishedQuery()
                .Where(x => x.Brand != null && x.Brand != "")
                .Select(x => x.Brand!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return Ok(brands);
        }

        [HttpGet("brand/{brand}")]
        public async Task<ActionResult<List<PublicSeoPageApiDto>>> ByBrand(
            string brand,
            [FromQuery] int count = 20)
        {
            count = NormalizeCount(count);

            var pages = await PublishedQuery()
                .Where(x => x.Brand != null && x.Brand == brand)
                .OrderByDescending(x => x.UpdatedAt)
                .Take(count)
                .ToListAsync();

            var categoryMap = await GetCategoryMapAsync();

            return Ok(pages.Select(x => ToListDto(x, categoryMap)).ToList());
        }

        [HttpGet("ai")]
        public async Task<ActionResult<List<PublicSeoPageAiApiDto>>> AiSearch(
            [FromQuery] string? q,
            [FromQuery] int count = 10)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Search query is required.");

            count = NormalizeCount(count);
            q = q.Trim();

            var pages = await PublishedQuery()
                .Where(x =>
                    x.Title.Contains(q) ||
                    x.TargetKeyword.Contains(q) ||
                    x.Slug.Contains(q) ||
                    x.MetaDescription != null && x.MetaDescription.Contains(q))
                .OrderByDescending(x => x.UpdatedAt)
                .Take(count)
                .ToListAsync();

            var categoryMap = await GetCategoryMapAsync();

            var result = pages.Select(x =>
            {
                Category? category = null;

                if (x.CategoryId.HasValue)
                    categoryMap.TryGetValue(x.CategoryId.Value, out category);

                return new PublicSeoPageAiApiDto
                {
                    Title = x.Title,
                    Summary = x.MetaDescription,
                    Url = BuildGuideUrl(x.Slug),
                    Category = category?.Name,
                    TargetKeyword = x.TargetKeyword
                };
            }).ToList();

            return Ok(result);
        }

        [HttpGet("{slug}")]
        public async Task<ActionResult<PublicSeoPageDetailApiDto>> BySlug(string slug)
        {
            var page = await PublishedQuery()
                .FirstOrDefaultAsync(x => x.Slug == slug);

            if (page == null)
                return NotFound();

            var categoryMap = await GetCategoryMapAsync();

            return Ok(ToDetailDto(page, categoryMap));
        }

        private IQueryable<SeoPage> PublishedQuery()
        {
            return _db.SeoPages
                .AsNoTracking()
                .Where(x => x.Status == 1);
        }

        private PublicSeoPageApiDto ToListDto(SeoPage page, Dictionary<int, Category> categoryMap)
        {
            Category? category = null;

            if (page.CategoryId.HasValue)
                categoryMap.TryGetValue(page.CategoryId.Value, out category);

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
                Url = BuildGuideUrl(page.Slug)
            };
        }

        private PublicSeoPageDetailApiDto ToDetailDto(SeoPage page, Dictionary<int, Category> categoryMap)
        {
            Category? category = null;

            if (page.CategoryId.HasValue)
                categoryMap.TryGetValue(page.CategoryId.Value, out category);

            return new PublicSeoPageDetailApiDto
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
                Url = BuildGuideUrl(page.Slug),
                IntroHtml = page.IntroHtml,
                BodyHtml = page.BodyHtml,
                FaqJson = page.FaqJson
            };
        }

        private string BuildGuideUrl(string slug)
        {
            return BuildAbsoluteUrl($"/guides/{slug}")!;
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

            var configuredBaseUrl = _configuration["PublicSite:BaseUrl"];

            var baseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
                ? $"{Request.Scheme}://{Request.Host}"
                : configuredBaseUrl.Trim().TrimEnd('/');

            return $"{baseUrl}/{value.TrimStart('/')}";
        }

        private static int NormalizeCount(int count)
        {
            if (count <= 0)
                return 10;

            if (count > 50)
                return 50;

            return count;
        }

        private static int NormalizePageSize(int pageSize)
        {
            if (pageSize < 1)
                return 20;

            if (pageSize > 100)
                return 100;

            return pageSize;
        }

        private async Task<Dictionary<int, Category>> GetCategoryMapAsync()
        {
            return await _db.Categories
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Id);
        }
    }
}