namespace AsMart.Web.Models.ViewModels;

public sealed class ProductGridVm
{
    public string? Title { get; set; }                      // e.g. Featured Products
    public IReadOnlyList<ProductCardVm> Products { get; set; } = Array.Empty<ProductCardVm>();

    // Buttons behavior differs by page
    public ProductGridActions Actions { get; set; } = ProductGridActions.DetailsOnly;

    // Optional URL at right side
    public string? ViewAllUrl { get; set; }

    // Optional: border style for "Deals"
    public bool HighlightDeals { get; set; } = false;
}

public enum ProductGridActions
{
    DetailsOnly,      // Home Featured, Purchases/Wishlist if you want only View
    DetailsAndBuy     // Catalog/Search/Category/Deals
}
