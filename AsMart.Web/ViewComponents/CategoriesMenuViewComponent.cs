using AsMart.Web.Models.ViewModels;
using AsMart.Web.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace AsMart.Web.ViewComponents
{
    public class CategoriesMenuViewComponent : ViewComponent
    {
        private const string CacheKey = "navbar_categories_menu_v1";

        private readonly IParentCategoryRepository _parentCategoryRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMemoryCache _cache;

        public CategoriesMenuViewComponent(
            IParentCategoryRepository parentCategoryRepository,
            ICategoryRepository categoryRepository,
            IMemoryCache cache)
        {
            _parentCategoryRepository = parentCategoryRepository;
            _categoryRepository = categoryRepository;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var tree = await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
                entry.SlidingExpiration = TimeSpan.FromMinutes(30);
                entry.Priority = CacheItemPriority.High;

                var parents = await _parentCategoryRepository.GetAllAsync();
                var categories = await _categoryRepository.GetAllEntitiesAsync();

                return parents
                    .Where(p => p.IsActive)
                    .OrderBy(p => (p.Name ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(p => new NavbarParentCategoryViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Slug = p.Slug,
                        Children = categories
                            .Where(c => c.IsActive && c.ParentCategoryId == p.Id)
                            .OrderBy(c => (c.Name ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                            .Select(c => new NavbarChildCategoryViewModel
                            {
                                Id = c.Id,
                                Name = c.Name,
                                Slug = c.Slug
                            })
                            .ToList()
                    })
                    .Where(p => p.Children.Any())
                    .ToList();
            });

            return View(tree ?? new List<NavbarParentCategoryViewModel>());
        }
    }
}