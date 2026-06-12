using System;

namespace AsMart.Web.Models.DTOs
{
    public class ProductListItemDto
    {
        public int Id { get; set; }
        public string ASIN { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public decimal? Price { get; set; }
        public string Currency { get; set; } = "USD";
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsDealOfTheDay { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? MainImageUrl { get; set; }
        public int ClickCount { get; set; }
    }
}
