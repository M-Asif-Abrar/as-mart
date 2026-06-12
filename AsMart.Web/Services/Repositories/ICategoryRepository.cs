// Services/Repositories/ICategoryRepository.cs
using AsMart.Web.Models.DTOs;
using AsMart.Web.Models.Entities;
using AsMart.Web.Models.ViewModels;

namespace AsMart.Web.Services.Repositories
{
    public interface ICategoryRepository
    {
        Task<List<CategoryListItemDto>> GetAllAsync();
        Task<CategoryDetailsDto?> GetDetailsAsync(int id);
        Task<CategoryFormDto?> GetForEditAsync(int id);
        Task<int> CreateAsync(CategoryFormDto dto);
        Task UpdateAsync(CategoryFormDto dto);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<List<Category>> GetAllEntitiesAsync();
        Task<Category?> GetBySlugAsync(string slug);
        Task<(List<Product> Products, int TotalCount)> GetPagedProductsByCategorySlugAsync(string slug);
        Task<List<Category>> GetActiveCategoriesForCatalogAsync();
        Task<Dictionary<int, int>> GetCategoryCountsForCatalogAsync();
        Task<List<BrandFilterOptionVm>> GetBrandOptionsForCatalogAsync();
    }
}