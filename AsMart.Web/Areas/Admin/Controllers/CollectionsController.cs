using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using AsMart.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CollectionsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CollectionsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Admin/Collections
        public async Task<IActionResult> Index()
        {
            var items = await _db.Collections
                .Select(c => new CollectionListItemViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ProductCount = c.CollectionProducts.Count
                })
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(items);
        }

        // GET: /Admin/Collections/Create
        public async Task<IActionResult> Create()
        {
            var vm = new CollectionFormViewModel
            {
                AllCategories = await _db.Categories
                    .OrderBy(c => c.Name)
                    .Select(c => new CategoryPickItem
                    {
                        Id = c.Id,
                        Name = c.Name
                    })
                    .ToListAsync(),

                // default initial list (can be filtered via button)
                AllProducts = await _db.Products
                    .OrderBy(p => p.Title)
                    .Select(p => new CollectionProductItem
                    {
                        Id = p.Id,
                        Title = p.Title
                    })
                    .ToListAsync()
            };

            return View(vm);
        }

        // GET: /Admin/Collections/FilterProducts?categoryIds=1&categoryIds=2&minPrice=50&maxPrice=100
        [HttpGet]
        public async Task<IActionResult> FilterProducts([FromQuery] List<int> categoryIds, decimal? minPrice, decimal? maxPrice)
        {
            var filtered = await BuildFilteredProductsQuery(categoryIds, minPrice, maxPrice)
                .OrderBy(p => p.Title)
                .Select(p => new CollectionProductItem
                {
                    Id = p.Id,
                    Title = p.Title
                })
                .ToListAsync();

            return PartialView("_CollectionProductCheckboxList", filtered);
        }

        // POST: /Admin/Collections/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CollectionFormViewModel vm)
        {
            if (vm.MinPrice.HasValue && vm.MaxPrice.HasValue && vm.MinPrice > vm.MaxPrice)
                ModelState.AddModelError(string.Empty, "Min price cannot be greater than max price.");

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(vm);
                await LoadProductsAsync(vm);
                return View(vm);
            }

            vm.Slug = vm.Slug.Trim();

            var exists = await _db.Collections.AnyAsync(c => c.Slug == vm.Slug);
            if (exists)
            {
                ModelState.AddModelError(nameof(vm.Slug), "A collection with this slug already exists.");
                await LoadCategoriesAsync(vm);
                await LoadProductsAsync(vm);
                return View(vm);
            }

            // manual selection
            var productIdsToAdd = (vm.SelectedProductIds ?? new()).Distinct().ToList();

            // auto-add from filter if nothing manually selected
            if (productIdsToAdd.Count == 0 && vm.AutoAddFilteredProducts)
            {
                productIdsToAdd = await BuildFilteredProductsQuery(vm.SelectedCategoryIds ?? new(), vm.MinPrice, vm.MaxPrice)
                    .Select(p => p.Id)
                    .Distinct()
                    .ToListAsync();
            }

            var collection = new Collection
            {
                Name = vm.Name,
                Slug = vm.Slug
            };

            _db.Collections.Add(collection);
            await _db.SaveChangesAsync();

            if (productIdsToAdd.Count > 0)
            {
                var links = productIdsToAdd.Select(pid => new CollectionProduct
                {
                    CollectionId = collection.Id,
                    ProductId = pid
                });

                _db.CollectionProducts.AddRange(links);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Collections/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var collection = await _db.Collections
                .Include(c => c.CollectionProducts)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (collection == null)
                return NotFound();

            var selectedProductIds = collection.CollectionProducts
                .Select(cp => cp.ProductId)
                .ToHashSet();

            var vm = new CollectionFormViewModel
            {
                Id = collection.Id,
                Name = collection.Name,
                Slug = collection.Slug,
                SelectedProductIds = selectedProductIds.ToList()
            };

            await LoadCategoriesAsync(vm);

            // show full list by default; admin can filter using button
            vm.AllProducts = await _db.Products
                .OrderBy(p => p.Title)
                .Select(p => new CollectionProductItem
                {
                    Id = p.Id,
                    Title = p.Title,
                    IsSelected = selectedProductIds.Contains(p.Id)
                })
                .ToListAsync();

            return View(vm);
        }

        // POST: /Admin/Collections/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CollectionFormViewModel vm)
        {
            if (vm.Id == null || id != vm.Id.Value)
                return NotFound();

            var collection = await _db.Collections
                .Include(c => c.CollectionProducts)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (collection == null)
                return NotFound();

            if (vm.MinPrice.HasValue && vm.MaxPrice.HasValue && vm.MinPrice > vm.MaxPrice)
                ModelState.AddModelError(string.Empty, "Min price cannot be greater than max price.");

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(vm);
                await LoadProductsAsync(vm);
                return View(vm);
            }

            vm.Slug = vm.Slug.Trim();

            var slugUsedByOther = await _db.Collections
                .AnyAsync(c => c.Id != id && c.Slug == vm.Slug);

            if (slugUsedByOther)
            {
                ModelState.AddModelError(nameof(vm.Slug), "A collection with this slug already exists.");
                await LoadCategoriesAsync(vm);
                await LoadProductsAsync(vm);
                return View(vm);
            }

            collection.Name = vm.Name;
            collection.Slug = vm.Slug;

            // manual selection
            var productIdsToApply = (vm.SelectedProductIds ?? new()).Distinct().ToList();

            // auto-add from filter if nothing manually selected
            if (productIdsToApply.Count == 0 && vm.AutoAddFilteredProducts)
            {
                productIdsToApply = await BuildFilteredProductsQuery(vm.SelectedCategoryIds ?? new(), vm.MinPrice, vm.MaxPrice)
                    .Select(p => p.Id)
                    .Distinct()
                    .ToListAsync();
            }

            // replace links
            _db.CollectionProducts.RemoveRange(collection.CollectionProducts);

            if (productIdsToApply.Count > 0)
            {
                var newLinks = productIdsToApply.Select(pid => new CollectionProduct
                {
                    CollectionId = collection.Id,
                    ProductId = pid
                });

                await _db.CollectionProducts.AddRangeAsync(newLinks);
            }

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Collections/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var collection = await _db.Collections
                .Include(c => c.CollectionProducts)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (collection == null)
                return NotFound();

            var vm = new CollectionListItemViewModel
            {
                Id = collection.Id,
                Name = collection.Name,
                Slug = collection.Slug,
                ProductCount = collection.CollectionProducts.Count
            };

            return View(vm);
        }

        // POST: /Admin/Collections/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var collection = await _db.Collections.FirstOrDefaultAsync(c => c.Id == id);

            if (collection != null)
            {
                _db.Collections.Remove(collection);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // --------------------
        // Helpers
        // --------------------

        private IQueryable<Product> BuildFilteredProductsQuery(List<int> categoryIds, decimal? minPrice, decimal? maxPrice)
        {
            var query = _db.Products.AsQueryable();

            // Price filtering
            if (minPrice.HasValue || maxPrice.HasValue)
                query = query.Where(p => p.Price.HasValue);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price!.Value >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price!.Value <= maxPrice.Value);

            // Category filtering (ALL selected categories)
            if (categoryIds != null && categoryIds.Count > 0)
            {
                var requiredCount = categoryIds.Distinct().Count();

                query = query.Where(p =>
                    _db.ProductCategories
                        .Where(pc => pc.ProductId == p.Id && categoryIds.Contains(pc.CategoryId))
                        .Select(pc => pc.CategoryId)
                        .Distinct()
                        .Count() == requiredCount
                );
            }


            // Optional: enforce USD only
            // query = query.Where(p => p.Currency == "USD");

            return query;
        }

        private async Task LoadCategoriesAsync(CollectionFormViewModel vm)
        {
            var selected = (vm.SelectedCategoryIds ?? new()).ToHashSet();

            vm.AllCategories = await _db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryPickItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsSelected = selected.Contains(c.Id)
                })
                .ToListAsync();
        }

        private async Task LoadProductsAsync(CollectionFormViewModel vm)
        {
            var selectedProducts = (vm.SelectedProductIds ?? new()).ToHashSet();

            var q = BuildFilteredProductsQuery(vm.SelectedCategoryIds ?? new(), vm.MinPrice, vm.MaxPrice);

            vm.AllProducts = await q
                .OrderBy(p => p.Title)
                .Select(p => new CollectionProductItem
                {
                    Id = p.Id,
                    Title = p.Title,
                    IsSelected = selectedProducts.Contains(p.Id)
                })
                .ToListAsync();
        }
    }
}
