using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.ViewModels
{
    public class CollectionListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public string? FirstProductImageUrl { get; set; }
    }

    public class CollectionFormViewModel
    {
        public int? Id { get; set; }

        [Required]
        [Display(Name = "Collection name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Slug (URL)")]
        public string Slug { get; set; } = string.Empty;

        // Manual selection
        public List<int> SelectedProductIds { get; set; } = new();

        // Products loaded (either full list or filtered list)
        public List<CollectionProductItem> AllProducts { get; set; } = new();

        // NEW: Category filter (multi-select)
        [Display(Name = "Categories")]
        public List<int> SelectedCategoryIds { get; set; } = new();

        public List<CategoryPickItem> AllCategories { get; set; } = new();

        // NEW: Price range
        [Display(Name = "Min price")]
        [Range(0, 999999)]
        public decimal? MinPrice { get; set; }

        [Display(Name = "Max price")]
        [Range(0, 999999)]
        public decimal? MaxPrice { get; set; }

        // NEW: Auto-add all filtered products if none manually selected
        [Display(Name = "Auto-add filtered products")]
        public bool AutoAddFilteredProducts { get; set; } = true;
    }

    public class CategoryPickItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class CollectionProductItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class CollectionDetailsViewModel
    {
        public int CollectionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        public List<CollectionProductCardViewModel> Products { get; set; } = new();
    }

    public class CollectionProductCardViewModel
    {
        public int ProductId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? MainImageUrl { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public decimal? Rating { get; set; }
        public string? ProductSlug { get; set; }
    }
}
