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
    public class ParentCategoriesController : Controller
    {
        private readonly IParentCategoryRepository _parentCategoryRepository;

        public ParentCategoriesController(IParentCategoryRepository parentCategoryRepository)
        {
            _parentCategoryRepository = parentCategoryRepository;
        }

        // GET: /Admin/ParentCategories
        public async Task<IActionResult> Index()
        {
            var parents = await _parentCategoryRepository.GetAllAsync();
            return View(parents);
        }

        // GET: /Admin/ParentCategories/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var details = await _parentCategoryRepository.GetDetailsAsync(id);
            if (details == null)
                return NotFound();

            return View(details);
        }

        // GET: /Admin/ParentCategories/Create
        public IActionResult Create()
        {
            var model = new ParentCategoryFormDto
            {
                IsActive = true,
                DisplayOrder = 0
            };
            return View(model);
        }

        // POST: /Admin/ParentCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ParentCategoryFormDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _parentCategoryRepository.CreateAsync(model);
            TempData["Success"] = "Parent category created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/ParentCategories/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _parentCategoryRepository.GetForEditAsync(id);
            if (model == null)
                return NotFound();

            return View(model);
        }

        // POST: /Admin/ParentCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ParentCategoryFormDto model)
        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!await _parentCategoryRepository.ExistsAsync(id))
                return NotFound();

            try
            {
                await _parentCategoryRepository.UpdateAsync(model);
                TempData["Success"] = "Parent category updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "An error occurred while updating the parent category.");
                return View(model);
            }
        }

        // GET: /Admin/ParentCategories/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var details = await _parentCategoryRepository.GetDetailsAsync(id);
            if (details == null)
                return NotFound();

            return View(details);
        }

        // POST: /Admin/ParentCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _parentCategoryRepository.DeleteAsync(id);
                TempData["Success"] = "Parent category deleted successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
