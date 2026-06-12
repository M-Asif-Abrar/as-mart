using AsMart.Web.Models.DTOs;

namespace AsMart.Web.Services.Repositories
{
    public interface IBlogRepository
    {
        Task<List<BlogPostSummaryDto>> GetPublishedPostsAsync(int page, int pageSize);
        Task<BlogPostDetailsDto?> GetPostBySlugAsync(string slug, string? currentUserId);

        Task<BlogPostEditDto?> GetForEditAsync(int id);
        Task<int> CreateAsync(BlogPostEditDto dto, string authorId);
        Task UpdateAsync(BlogPostEditDto dto, string authorId);
        Task DeleteAsync(int id);

        Task RateAsync(int postId, string userId, byte rating);
    }
}
