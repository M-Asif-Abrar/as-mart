using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.DTOs
{
    public class ProductFormDto
    {
        public int? Id { get; set; } // null for create

        [Required]
        [Display(Name = "ASIN Code")]
        public string ASIN { get; set; } = null!;

        [Required]
        [Display(Name = "Product Title")]
        public string Title { get; set; } = null!;

        [Display(Name = "Short Description")]
        public string? ShortDescription { get; set; }

        [Display(Name = "Full Description")]
        public string? Description { get; set; }

        [Display(Name = "Brand Name")]
        public string? Brand { get; set; }

        [Display(Name = "Sale Price")]
        public decimal? Price { get; set; }

        [Display(Name = "List Price")]
        public decimal? ListPrice { get; set; }

        [Display(Name = "Currency")]
        public string Currency { get; set; } = "USD";

        [Display(Name = "Main Image URL")]
        public string? MainImageUrl { get; set; }

        [Display(Name = "Affiliate URL Override")]
        public string? AffiliateUrlOverride { get; set; }

        [Display(Name = "Featured Product")]
        public bool IsFeatured { get; set; }

        [Display(Name = "Deal of the Day")]
        public bool IsDealOfTheDay { get; set; }

        [Display(Name = "Slug")]
        public string? Slug { get; set; }

        [Display(Name = "Active Product")]
        public bool IsActive { get; set; } = true;

        // NEW (Step 1) --- Add Ratings fields
        [Display(Name = "Rating (0–5)")]
        public decimal? Rating { get; set; }

        [Display(Name = "Rating Count")]
        public int? RatingCount { get; set; }

        // Categories
        public List<int> SelectedCategoryIds { get; set; } = new List<int>();

        // Additional Images
        [Display(Name = "Additional Images (URLs)")]
        public string? AdditionalImageUrls { get; set; }
    }
}
