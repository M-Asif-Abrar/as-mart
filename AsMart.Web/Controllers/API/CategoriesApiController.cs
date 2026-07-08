using AsMart.Web.Data;
using AsMart.Web.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace AsMart.Web.Controllers.Api
{
    [ApiController]
    [Route("api/categories")]
    [Produces("application/json")]
    [EnableRateLimiting("public-api")]
    public class CategoriesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CategoriesApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<PublicCategoryApiDto>>> GetCategories()
        {
            var categories = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new PublicCategoryApiDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ProductCount = c.ProductCategories.Count(pc => pc.Product.IsActive)
                })
                .ToListAsync();

            return Ok(categories);
        }
    }
}