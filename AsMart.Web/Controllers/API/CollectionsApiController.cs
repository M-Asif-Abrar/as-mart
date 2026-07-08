using AsMart.Web.Data;
using AsMart.Web.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace AsMart.Web.Controllers.Api
{
    [ApiController]
    [Route("api/collections")]
    [Produces("application/json")]
    [EnableRateLimiting("public-api")]
    public class CollectionsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CollectionsApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<PublicCollectionApiDto>>> GetCollections()
        {
            var collections = await _db.Collections
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new PublicCollectionApiDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ProductCount = c.CollectionProducts.Count(cp => cp.Product.IsActive)
                })
                .ToListAsync();

            return Ok(collections);
        }
    }
}