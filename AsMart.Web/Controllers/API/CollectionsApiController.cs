using AsMart.Web.Data;
using AsMart.Web.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Controllers.Api
{
    [ApiController]
    [Route("api/collections")]
    [Route("api/v1/collections")]
    [Produces("application/json")]
    [EnableRateLimiting("public-api")]
    public sealed class CollectionsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CollectionsApiController(
            ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [ProducesResponseType(
            typeof(List<PublicCollectionApiDto>),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PublicCollectionApiDto>>>
            GetCollections(
                CancellationToken cancellationToken)
        {
            var collections = await _db.Collections
                .AsNoTracking()
                .OrderBy(collection => collection.Name)
                .Select(collection => new PublicCollectionApiDto
                {
                    Id = collection.Id,
                    Name = collection.Name,
                    Slug = collection.Slug,

                    ProductCount =
                        collection.CollectionProducts.Count(
                            collectionProduct =>
                                collectionProduct.Product.IsActive)
                })
                .ToListAsync(cancellationToken);

            return Ok(collections);
        }
    }
}