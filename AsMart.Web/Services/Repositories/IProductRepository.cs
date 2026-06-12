using System.Collections.Generic;
using System.Threading.Tasks;
using AsMart.Web.Models.DTOs;
using AsMart.Web.Models.Entities;

namespace AsMart.Web.Services.Repositories
{
    public interface IProductRepository
    {
        Task<List<ProductListItemDto>> GetAllAsync();
        Task<ProductDetailsDto?> GetDetailsAsync(int id);
        Task<ProductFormDto?> GetForEditAsync(int id);
        Task<int> CreateAsync(ProductFormDto dto);
        Task UpdateAsync(ProductFormDto dto);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<Product?> GetEntityByIdAsync(int id);
        Task<Product?> GetBySlugAsync(string slug);

        Task<IReadOnlyList<Product>> GetRelatedProductsAsync(int productId, int maxItems = 12);

        Task<IReadOnlyList<Product>> GetOtherProductsAsync(int productId, int maxItems = 12);
    }
}
