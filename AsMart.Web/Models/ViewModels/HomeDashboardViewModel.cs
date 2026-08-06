using AsMart.Web.Models.Entities;

namespace AsMart.Web.Models.ViewModels;

public sealed class HomeDashboardViewModel
{
    public IReadOnlyCollection<Product> FeaturedProducts { get; init; }
        = Array.Empty<Product>();

    public int TotalProducts { get; init; }

    public int FeaturedProductsCount { get; init; }

    public int DealsCount { get; init; }

    public long TotalProductViews { get; init; }

    public decimal AverageRating { get; init; }
}