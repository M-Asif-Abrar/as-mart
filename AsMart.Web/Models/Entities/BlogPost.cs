using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.Entities
{
    public class BlogPost
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;
        // SEO
        [MaxLength(256)]
        public string? MetaTitle { get; set; }

        [MaxLength(512)]
        public string? MetaDescription { get; set; }

        [MaxLength(512)]
        public string? OgImageUrl { get; set; }

        public string Slug { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? FeaturedImageUrl { get; set; }
        [MaxLength(512)]
        public string? ProductPageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public bool IsPublished { get; set; }

        public string? AuthorId { get; set; }
        public ApplicationUser? Author { get; set; }

        // Aggregated rating
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }

        // Navigation
        public ICollection<BlogPostCategory> BlogPostCategories { get; set; } = new List<BlogPostCategory>();
        public ICollection<BlogPostTag> BlogPostTags { get; set; } = new List<BlogPostTag>();
        public ICollection<BlogPostRating> Ratings { get; set; } = new List<BlogPostRating>();
        public ICollection<ClickLog> ClickLogs { get; set; } = new List<ClickLog>();
    }
}
