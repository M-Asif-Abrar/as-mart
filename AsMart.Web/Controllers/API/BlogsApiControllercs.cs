using AsMart.Web.Data;
using AsMart.Web.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace AsMart.Web.Controllers.Api
{
    [ApiController]
    [Route("api/blogs")]
    [Produces("application/json")]
    [EnableRateLimiting("public-api")]
    public class BlogsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public BlogsApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /api/blogs/latest?count=6
        [HttpGet("latest")]
        public async Task<ActionResult<List<PublicBlogApiDto>>> Latest([FromQuery] int count = 6)
        {
            count = NormalizeCount(count);

            var blogEntities = await _db.BlogPosts
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .ToListAsync();

            var blogs = blogEntities.Select(b => new PublicBlogApiDto
            {
                Id = b.Id,
                Title = b.Title,
                Slug = b.Slug,
                MetaDescription = b.MetaDescription,
                OgImageUrl = b.OgImageUrl,
                BlogUrl = Url.Action(
                    "Details",
                    "Blog",
                    new { slug = b.Slug },
                    Request.Scheme
                ) ?? $"/blog/{b.Slug}"
            }).ToList();

            return Ok(blogs);
        }

        private static int NormalizeCount(int count)
        {
            if (count <= 0)
                return 6;

            if (count > 50)
                return 50;

            return count;
        }
    }
}