using System.Collections.Generic;
using System.Threading.Tasks;
using AsMart.Web.Models.DTOs;
using AsMart.Web.Models.Entities;

namespace AsMart.Web.Services.Repositories
{
    public interface IParentCategoryRepository
    {
        Task<List<ParentCategoryListItemDto>> GetAllAsync();
        Task<ParentCategoryDetailsDto?> GetDetailsAsync(int id);
        Task<ParentCategoryFormDto?> GetForEditAsync(int id);
        Task<int> CreateAsync(ParentCategoryFormDto dto);
        Task UpdateAsync(ParentCategoryFormDto dto);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);

        // For internal use if needed
        Task<List<Category>> GetAllParentEntitiesAsync();
    }
}
