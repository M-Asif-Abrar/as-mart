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
    [Route("api/v{version:apiVersion}/categories")]
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

        // GET: /api/v1/categories
        [HttpGet]
        [MapToApiVersion("1.0")]
        [ProducesResponseType(
            typeof(List<PublicCategoryApiDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<PublicCategoryApiDto>>>
            GetCategories(
                CancellationToken cancellationToken = default)
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