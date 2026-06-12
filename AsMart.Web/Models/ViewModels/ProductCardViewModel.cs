// Models/ViewModels/ProductCardViewModel.cs
using System;
using AsMart.Web.Models.Entities;

namespace AsMart.Web.Models.ViewModels
{
    public class ProductCardViewModel
    {
        public int Id { get; set; }
        public string Slug { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? MainImageUrl { get; set; }

        public decimal? Price { get; set; }
        public decimal? ListPrice { get; set; }
        public string? Currency { get; set; }

        public decimal? Rating { get; set; }
        public int? RatingCount { get; set; }

        public string? PrimaryCategoryName { get; set; }
    }
}
