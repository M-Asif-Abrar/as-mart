using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.DTOs
{
    public class BlogPostSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? FeaturedImageUrl { get; set; }
        public string? Excerpt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public int ViewCount { get; set; }

        public List<string> CategoryNames { get; set; } = new();
        public List<string> TagNames { get; set; } = new();
    }

    public class BlogPostDetailsDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? FeaturedImageUrl { get; set; }
        public DateTime? PublishedAt { get; set; }

        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? OgImageUrl { get; set; }

        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public byte? CurrentUserRating { get; set; }

        public string? AuthorName { get; set; }

        // Buy button
        public string? ProductPageUrl { get; set; }

        public List<string> CategoryNames { get; set; } = new();
        public List<string> TagNames { get; set; } = new();
    }


    public class BlogPostEditDto
    {
        public int? Id { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? FeaturedImageUrl { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }

        [Display(Name = "Product page URL (optional)")]
        [MaxLength(512)]
        public string? ProductPageUrl { get; set; }

        public List<int> SelectedCategoryIds { get; set; } = new();
        public List<int> SelectedTagIds { get; set; } = new();
    }
}
