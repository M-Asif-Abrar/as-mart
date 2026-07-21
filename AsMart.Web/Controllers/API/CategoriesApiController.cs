using AsMart.Web.Data;
using AsMart.Web.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Controllers.Api
{
    [ApiController]
    [Route("api/categories")]
    [Route("api/v1/categories")]
    [Produces("application/json")]
    [EnableRateLimiting("public-api")]
    public sealed class CategoriesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CategoriesApiController(
            ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<PublicCategoryApiDto>>>
            GetCategories(
                CancellationToken cancellationToken)
        {
            var categories = await _db.Categories
                .AsNoTracking()
                .OrderBy(category => category.Name)
                .Select(category => new PublicCategoryApiDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Slug = category.Slug,

                    ProductCount =
                        category.ProductCategories.Count(
                            productCategory =>
                                productCategory.Product.IsActive)
                })
                .ToListAsync(cancellationToken);

            return Ok(categories);
        }
    }
}