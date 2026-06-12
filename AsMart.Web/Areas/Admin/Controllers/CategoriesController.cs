using System.Threading.Tasks;
using AsMart.Web.Models.DTOs;
using AsMart.Web.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Editor")]
    public class CategoriesController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoriesController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        // GET: /Admin/Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return View(categories);
        }

        // GET: /Admin/Categories/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var details = await _categoryRepository.GetDetailsAsync(id);
            if (details == null)
                return NotFound();

            return View(details);
        }

        // GET: /Admin/Categories/Create
        public async Task<IActionResult> Create()
        {
            await LoadParentCategoriesAsync();
            var model = new CategoryFormDto
            {
                IsActive = true,
                DisplayOrder = 0
            };
            return View(model);
        }

        // POST: /Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryFormDto model)
        {
            if (!ModelState.IsValid)
            {
                await LoadParentCategoriesAsync();
                return View(model);
            }

            await _categoryRepository.CreateAsync(model);
            TempData["Success"] = "Category created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Categories/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _categoryRepository.GetForEditAsync(id);
            if (model == null)
                return NotFound();

            await LoadParentCategoriesAsync(id);
            return View(model);
        }

        // POST: /Admin/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryFormDto model)
        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadParentCategoriesAsync(id);
                return View(model);
            }

            if (!await _categoryRepository.ExistsAsync(id))
                return NotFound();

            try
            {
                await _categoryRepository.UpdateAsync(model);
                TempData["Success"] = "Category updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "An error occurred while updating the category.");
                await LoadParentCategoriesAsync(id);
                return View(model);
            }
        }

        // GET: /Admin/Categories/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var details = await _categoryRepository.GetDetailsAsync(id);
            if (details == null)
                return NotFound();

            return View(details);
        }

        // POST: /Admin/Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _categoryRepository.DeleteAsync(id);
                TempData["Success"] = "Category deleted successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadParentCategoriesAsync(int? excludeId = null)
        {
            var all = await _categoryRepository.GetAllEntitiesAsync();

            var parentOptions = all
                .Where(c => !excludeId.HasValue || c.Id != excludeId.Value)
                .ToList();

            ViewBag.ParentCategories = parentOptions;
        }
    }
}
