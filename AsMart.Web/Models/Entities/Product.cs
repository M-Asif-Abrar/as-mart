using AsMart.Web.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.Entities
{
    public class Product
    {
        public int Id { get; set; }

        [Display(Name = "ASIN Code")]
        public string ASIN { get; set; } = null!;

        [Display(Name = "Product Title")]
        public string Title { get; set; } = null!;

        [Display(Name = "SEO Slug")]
        public string Slug { get; set; } = null!;

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

        [Display(Name = "Rating")]
        public decimal? Rating { get; set; }

        [Display(Name = "Rating Count")]
        public int? RatingCount { get; set; }

        [Display(Name = "Main Image URL")]
        public string? MainImageUrl { get; set; }

        [Display(Name = "Additional Images (JSON)")]
        public string? AdditionalImagesJson { get; set; }

        [Display(Name = "Affiliate URL Override")]
        public string? AffiliateUrlOverride { get; set; }

        [Display(Name = "Featured Product")]
        public bool IsFeatured { get; set; }

        [Display(Name = "Active Product")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Deal of the Day")]
        public bool IsDealOfTheDay { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; }

        [Display(Name = "Last Synced At")]
        public DateTime? LastSyncedAt { get; set; }

        [Display(Name = "Total Clicks")]
        public int ClickCount { get; set; }

        public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
        public ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();
        public ICollection<CollectionProduct> CollectionProducts { get; set; } = new List<CollectionProduct>();
        public ICollection<ClickLog> ClickLogs { get; set; } = new List<ClickLog>();
        
    }
}