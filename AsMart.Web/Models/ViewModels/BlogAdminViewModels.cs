using System;
using System.Collections.Generic;
using AsMart.Web.Models.DTOs;

namespace AsMart.Web.Models.ViewModels
{
    public class AdminBlogPostListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public string? FeaturedImageUrl { get; set; }
        public int ClicksLast30Days { get; set; }
        public int TotalClicks { get; set; }


    }
}
