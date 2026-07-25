using Asp.Versioning;
using AsMart.Web.Data;
using AsMart.Web.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Controllers.API.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/blogs")]
    [Produces("application/json")]
    [EnableRateLimiting("public-api")]
    public sealed class BlogsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;

        public BlogsApiController(
            ApplicationDbContext db,
            IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        // GET: /api/v1/blogs/latest?count=6
        [HttpGet("latest")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType(
            typeof(List<PublicBlogApiDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<PublicBlogApiDto>>> Latest(
            [FromQuery] int count = 6,
            CancellationToken cancellationToken = default)
        {
            count = NormalizeCount(count);

            var blogEntities = await _db.BlogPosts
                .AsNoTracking()
                .Where(blog => blog.IsPublished)
                .OrderByDescending(blog =>
                    blog.PublishedAt ??
                    blog.UpdatedAt ??
                    blog.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);

            var blogs = blogEntities
                .Select(blog => new PublicBlogApiDto
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
                        $"/blog/{Uri.EscapeDataString(blog.Slug.Trim())}")!
                })
                .ToList();

            return Ok(blogs);
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

            var configuredBaseUrl =
                _configuration["PublicSite:BaseUrl"];

            var baseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
                ? $"{Request.Scheme}://{Request.Host}"
                : configuredBaseUrl.Trim().TrimEnd('/');

            return $"{baseUrl}/{value.TrimStart('/')}";
        }

        private static int NormalizeCount(int count)
        {
            return Math.Clamp(
                count <= 0 ? 6 : count,
                1,
                50);
        }
    }
}