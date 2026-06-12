using System;
using System.Linq;
using System.Threading.Tasks;
using AsMart.Web.Data;
using AsMart.Web.Models.DTOs;
using AsMart.Web.Models.Entities;
using AsMart.Web.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Editor")]
    public class ProductsController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _db;

        public ProductsController(IProductRepository productRepository, ApplicationDbContext db)
        {
            _productRepository = productRepository;
            _db = db;
        }

        // GET: /Admin/Products
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        // GET: /Admin/Products/Create
        public async Task<IActionResult> Create()
        {
            await LoadCategoriesAsync();
            await LoadCategoryGroupsAsync();

            var model = new ProductFormDto
            {
                IsActive = true,
                Currency = "USD",
                SelectedCategoryIds = modelSelectedCategoryIdsSafe()
            };

            return View(model);

            static System.Collections.Generic.List<int> modelSelectedCategoryIdsSafe()
                => new System.Collections.Generic.List<int>();
        }

        // POST: /Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormDto model)
        {
            // Ensure list is not null (prevents null reference in view)
            model.SelectedCategoryIds ??= new System.Collections.Generic.List<int>();

            // Normalize inputs (prevents duplicates by whitespace/case)
            model.Title = (model.Title ?? string.Empty).Trim();
            model.Slug = (model.Slug ?? string.Empty).Trim();
            model.ASIN = (model.ASIN ?? string.Empty).Trim();

            // If your app generates slug automatically when empty, you can still keep it empty here.
            // We'll validate slug only if it is provided (or if your DTO always has slug).
            // If you ALWAYS generate slug in repo, keep slug optional.

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return View(model);
            }

            // -------------------------
            // Friendly Duplicate Checks
            // -------------------------
            // Title (required usually)
            if (!string.IsNullOrWhiteSpace(model.Title))
            {
                var existsTitle = await _db.Products.AsNoTracking()
                    .AnyAsync(p => p.Title != null && p.Title.ToLower() == model.Title.ToLower());

                if (existsTitle)
                    ModelState.AddModelError(nameof(model.Title), "A product already exists with the same Title.");
            }

            // Slug (only check if user provided it)
            if (!string.IsNullOrWhiteSpace(model.Slug))
            {
                var existsSlug = await _db.Products.AsNoTracking()
                    .AnyAsync(p => p.Slug != null && p.Slug.ToLower() == model.Slug.ToLower());

                if (existsSlug)
                    ModelState.AddModelError(nameof(model.Slug), "A product already exists with the same Slug.");
            }

            // ASIN (often unique)
            if (!string.IsNullOrWhiteSpace(model.ASIN))
            {
                var existsAsin = await _db.Products.AsNoTracking()
                    .AnyAsync(p => p.ASIN != null && p.ASIN.ToLower() == model.ASIN.ToLower());

                if (existsAsin)
                    ModelState.AddModelError(nameof(model.ASIN), "A product already exists with the same ASIN.");
            }

            if (!ModelState.IsValid)
            {
                // A summary error helps user understand quickly
                ModelState.AddModelError(string.Empty, "Duplicate detected. Please use a unique Title, Slug, and ASIN.");
                await LoadCategoriesAsync();
                return View(model);
            }

            // -------------------------
            // Save (with safety-net)
            // -------------------------
            try
            {
                await _productRepository.CreateAsync(model);

                TempData["Success"] = "Product created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                // If DB unique constraint hits (race condition OR repo slug generation),
                // show user-friendly message (no HTTP 500).
                ModelState.AddModelError(string.Empty,
                    "This product already exists (duplicate Title, Slug, or ASIN). Please search and edit the existing product.");

                await LoadCategoriesAsync();
                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty,
                    "Unable to create product due to an unexpected error. Please try again.");

                await LoadCategoriesAsync();
                await LoadCategoryGroupsAsync();
                return View(model);
            }
        }

        // GET: /Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _productRepository.GetForEditAsync(id);
            if (model == null)
                return NotFound();

            model.SelectedCategoryIds ??= new System.Collections.Generic.List<int>();

            await LoadCategoriesAsync();
            return View(model);
        }

        // POST: /Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductFormDto model)
        {
            if (id != model.Id)
                return BadRequest();

            model.SelectedCategoryIds ??= new System.Collections.Generic.List<int>();

            model.Title = (model.Title ?? string.Empty).Trim();
            model.Slug = (model.Slug ?? string.Empty).Trim();
            model.ASIN = (model.ASIN ?? string.Empty).Trim();

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return View(model);
            }

            var exists = await _productRepository.ExistsAsync(id);
            if (!exists)
                return NotFound();

            // Duplicate checks excluding current record
            if (!string.IsNullOrWhiteSpace(model.Title))
            {
                var existsTitle = await _db.Products.AsNoTracking()
                    .AnyAsync(p => p.Id != id && p.Title != null && p.Title.ToLower() == model.Title.ToLower());

                if (existsTitle)
                    ModelState.AddModelError(nameof(model.Title), "Another product already exists with the same Title.");
            }

            if (!string.IsNullOrWhiteSpace(model.Slug))
            {
                var existsSlug = await _db.Products.AsNoTracking()
                    .AnyAsync(p => p.Id != id && p.Slug != null && p.Slug.ToLower() == model.Slug.ToLower());

                if (existsSlug)
                    ModelState.AddModelError(nameof(model.Slug), "Another product already exists with the same Slug.");
            }

            if (!string.IsNullOrWhiteSpace(model.ASIN))
            {
                var existsAsin = await _db.Products.AsNoTracking()
                    .AnyAsync(p => p.Id != id && p.ASIN != null && p.ASIN.ToLower() == model.ASIN.ToLower());

                if (existsAsin)
                    ModelState.AddModelError(nameof(model.ASIN), "Another product already exists with the same ASIN.");
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Duplicate detected. Please use a unique Title, Slug, and ASIN.");
                await LoadCategoriesAsync();
                return View(model);
            }

            try
            {
                await _productRepository.UpdateAsync(model);

                TempData["Success"] = "Product updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty,
                    "Update failed because a product with the same Title, Slug, or ASIN already exists.");

                await LoadCategoriesAsync();
                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty,
                    "Unable to update product due to an unexpected error. Please try again.");

                await LoadCategoriesAsync();
                return View(model);
            }
        }

        // GET: /Admin/Products/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var details = await _productRepository.GetDetailsAsync(id);
            if (details == null)
                return NotFound();

            return View(details);
        }

        // GET: /Admin/Products/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var details = await _productRepository.GetDetailsAsync(id);
            if (details == null)
                return NotFound();

            return View(details);
        }

        // POST: /Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productRepository.DeleteAsync(id);

            TempData["Success"] = "Product deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Load active categories into ViewBag for forms
        private async Task LoadCategoriesAsync()
        {
            ViewBag.Categories = await _db.Categories
                .AsNoTracking()
                .Include(c => c.ParentCategory)
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
        }


        private async Task LoadCategoryGroupsAsync()
        {
            // Get active categories
            var categories = await _db.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .ToListAsync();

            // Group by ParentCategory (heading) and sort A–Z
            var groups = categories
                .GroupBy(c => new { c.ParentCategoryId, ParentName = c.ParentCategory != null ? c.ParentCategory.Name : "" })
                .Select(g => new
                {
                    ParentId = g.Key.ParentCategoryId ?? 0,
                    ParentName = (g.Key.ParentName ?? "").Trim(),
                    Children = g
                        .OrderBy(x => (x.Name ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                        .Select(x => new { x.Id, Name = (x.Name ?? "").Trim() })
                        .ToList()
                })
                .Where(g => !string.IsNullOrWhiteSpace(g.ParentName) && g.Children.Any())
                .OrderBy(g => g.ParentName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            ViewBag.CategoryGroups = groups;
        }

    }
}
