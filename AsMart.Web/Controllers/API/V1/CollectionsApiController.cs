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
    [Route("api/v{version:apiVersion}/collections")]
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

        // GET: /api/v1/collections
        [HttpGet]
        [MapToApiVersion("1.0")]
        [ProducesResponseType(
            typeof(List<PublicCollectionApiDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<PublicCollectionApiDto>>>
            GetCollections(
                CancellationToken cancellationToken = default)
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