using AsMart.Web.Models.ViewModels.Gallery;

namespace AsMart.Web.Services.Gallery
{
    public interface IGalleryService
    {
        Task<GalleryIndexViewModel> GetGalleryAsync(
            GalleryQueryViewModel query,
            CancellationToken cancellationToken = default);
    }
}