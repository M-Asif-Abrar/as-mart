using AsMart.Web.Models.Entities;
using AsMart.Web.Models.ViewModels;

namespace AsMart.Web.Helpers;

public static class ProductVmMapper
{
    public static ProductCardVm ToCardVm(this Product p, string? categoryNameForTracking = null)
    {
        return new ProductCardVm
        {
            Id = p.Id,
            Slug = p.Slug,
            Title = p.Title,
            ImageUrl = string.IsNullOrWhiteSpace(p.MainImageUrl) ? "/images/no-image.png" : p.MainImageUrl,
            Price = p.Price,
            ListPrice = p.ListPrice,
            Currency = string.IsNullOrWhiteSpace(p.Currency) ? "USD" : p.Currency,
            Rating = p.Rating ?? 0,              // adjust if your entity uses nullable
            RatingCount = p.RatingCount ?? 0,    // adjust if your entity uses nullable
            ClickCount = p.ClickCount,
            CategoryNameForTracking = categoryNameForTracking
        };
    }
}
