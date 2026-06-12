using System.Linq;
using System.Threading.Tasks;
using AsMart.Web.Data;
using AsMart.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Controllers
{
    public class CollectionsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CollectionsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Collections
        public async Task<IActionResult> Index()
        {
            var items = await _db.Collections
                .Select(c => new CollectionListItemViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ProductCount = c.CollectionProducts.Count,
                    FirstProductImageUrl = c.CollectionProducts
                    .OrderBy(x => Guid.NewGuid())
                    .Select(cp => cp.Product.MainImageUrl)
                    .FirstOrDefault()
                })
                .OrderByDescending(c => c.ProductCount)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(items);
        }

        // GET: /Collections/{slug}
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return NotFound();

            var collection = await _db.Collections
                .Include(c => c.CollectionProducts)
                    .ThenInclude(cp => cp.Product)
                .FirstOrDefaultAsync(c => c.Slug == slug);

            if (collection == null)
                return NotFound();

            var vm = new CollectionDetailsViewModel
            {
                CollectionId = collection.Id,
                Name = collection.Name,
                Slug = collection.Slug,
                Products = collection.CollectionProducts
                    .Where(cp => cp.Product != null)
                    .Select(cp => cp.Product!)
                    .Select(p => new CollectionProductCardViewModel
                    {
                        ProductId = p.Id,
                        Title = p.Title,
                        MainImageUrl = p.MainImageUrl,
                        Price = p.Price,
                        Currency = p.Currency,
                        Rating = p.Rating,
                        ProductSlug = p.Slug
                    })
                    .ToList()
            };

            return View(vm);
        }
    }
}
