using System;
using System.Collections.Generic;

namespace AsMart.Web.Models.DTOs
{
    public class ProductDetailsDto
    {
        public int Id { get; set; }
        public string ASIN { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public decimal? Price { get; set; }
        public decimal? ListPrice { get; set; }
        public string Currency { get; set; } = "USD";
        public string? MainImageUrl { get; set; }
        public string? AdditionalImagesJson { get; set; }
        public string? AffiliateUrlOverride { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsDealOfTheDay { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int ClickCount { get; set; }

        public List<string> CategoryNames { get; set; } = new List<string>();
    }
}
