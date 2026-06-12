// Controllers/MyProductsController.cs
using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Controllers
{
    [Authorize]
    public class MyProductsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MyProductsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPurchased(int productId)
        {
            var userId = User.GetUserId()!;
            var product = await _db.Products.FindAsync(productId);

            if (product == null || !product.IsActive)
                return NotFound();

            // Remove old "MarkedPurchased" for this user/product (if you only want one)
            var old = await _db.UserProductStatuses
                .Where(x => x.UserId == userId &&
                            x.ProductId == productId &&
                            x.State == UserProductState.MarkedPurchased)
                .ToListAsync();

            _db.UserProductStatuses.RemoveRange(old);

            // Add new record
            var status = new UserProductStatus
            {
                UserId = userId,
                ProductId = productId,
                State = UserProductState.MarkedPurchased,
                CreatedAt = DateTime.UtcNow
            };

            _db.UserProductStatuses.Add(status);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Product marked as purchased.";
            return RedirectToAction("Details", "Product", new { slug = product.Slug });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToWishlist(int productId)
        {
            var userId = User.GetUserId()!;
            var product = await _db.Products.FindAsync(productId);

            if (product == null || !product.IsActive)
                return NotFound();

            bool exists = await _db.UserProductStatuses
                .AnyAsync(x => x.UserId == userId &&
                               x.ProductId == productId &&
                               x.State == UserProductState.Wishlisted);

            if (!exists)
            {
                var status = new UserProductStatus
                {
                    UserId = userId,
                    ProductId = productId,
                    State = UserProductState.Wishlisted,
                    CreatedAt = DateTime.UtcNow
                };
                _db.UserProductStatuses.Add(status);
                await _db.SaveChangesAsync();
            }

            TempData["Message"] = "Product added to your wishlist.";
            return RedirectToAction("Details", "Product", new { slug = product.Slug });
        }

        [HttpGet]
        public async Task<IActionResult> Purchases()
        {
            var userId = User.GetUserId()!;

            var products = await _db.UserProductStatuses
                .Where(x => x.UserId == userId &&
                            x.State == UserProductState.MarkedPurchased)
                .Include(x => x.Product)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.Product)
                .ToListAsync();

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Wishlist()
        {
            var userId = User.GetUserId()!;

            var products = await _db.UserProductStatuses
                .Where(x => x.UserId == userId &&
                            x.State == UserProductState.Wishlisted)
                .Include(x => x.Product)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.Product)
                .ToListAsync();

            return View(products);
        }
    }
}
