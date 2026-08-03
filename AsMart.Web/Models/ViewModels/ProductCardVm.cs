namespace AsMart.Web.Models.ViewModels;

public sealed class ProductCardVm
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public decimal? Price { get; set; }
    public decimal? ListPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal Rating { get; set; }
    public int RatingCount { get; set; }
    public int ClickCount { get; init; }

    // Optional for tracking
    public string? CategoryNameForTracking { get; set; }

    public decimal? DiscountPercent =>
        (Price.HasValue && ListPrice.HasValue && ListPrice.Value > Price.Value && ListPrice.Value > 0)
            ? Math.Round((ListPrice.Value - Price.Value) / ListPrice.Value * 100m, 0)
            : null;
}
