// Models/ViewModels/AdminDashboardViewModel.cs
using System;
using System.Collections.Generic;

namespace AsMart.Web.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalBlogPosts { get; set; }
        public int TotalCollections { get; set; }
        public int TotalClickLogs { get; set; }
        public int TotalUserProductStatuses { get; set; }

        // Click metrics
        public int ClicksToday { get; set; }
        public int ClicksLast7Days { get; set; }
        public int ClicksLast30Days { get; set; }

        // Existing lists
        public List<CategoryProductsItem> ProductsPerCategory { get; set; } = new();
        public List<CollectionProductsItem> ProductsPerCollection { get; set; } = new();

        // New: top clicked products / categories
        public List<TopProductClicksItem> TopProductsByClicks { get; set; } = new();
        public List<TopCategoryClicksItem> TopCategoriesByClicks { get; set; } = new();

        public class CategoryProductsItem
        {
            public string CategoryName { get; set; } = string.Empty;
            public int ProductCount { get; set; }
        }

        public class CollectionProductsItem
        {
            public string CollectionName { get; set; } = string.Empty;
            public int ProductCount { get; set; }
        }

        public class TopProductClicksItem
        {
            public int ProductId { get; set; }
            public string Slug { get; set; } = null!;
            public string Title { get; set; } = string.Empty;
            public string? CategoryName { get; set; }
            public int Clicks { get; set; }
            public DateTime? LastClickedAt { get; set; }
        }

        public class TopCategoryClicksItem
        {
            public string CategoryName { get; set; } = string.Empty;
            public int Clicks { get; set; }
        }
    }
}
