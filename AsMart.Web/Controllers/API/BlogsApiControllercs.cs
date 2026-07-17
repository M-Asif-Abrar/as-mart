using AsMart.Web.Data;
using AsMart.Web.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Controllers.Api
{
    [ApiController]
    [Route("api/blogs")]
    [Produces("application/json")]
    [EnableRateLimiting("public-api")]
    public sealed class BlogsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public BlogsApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /api/blogs/latest?count=6
        [HttpGet("latest")]
        [ProducesResponseType(
            typeof(List<PublicBlogApiDto>),
            StatusCodes.Status200OK)]
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

                    BlogUrl = BuildBlogUrl(blog.Slug)
                })
                .ToList();

            return Ok(blogs);
        }

        private string BuildBlogUrl(string slug)
        {
            var encodedSlug = Uri.EscapeDataString(
                slug.Trim());

            return string.Concat(
                Request.Scheme,
                "://",
                Request.Host,
                "/blog/",
                encodedSlug);
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

            return string.Concat(
                Request.Scheme,
                "://",
                Request.Host,
                "/",
                value.TrimStart('/'));
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