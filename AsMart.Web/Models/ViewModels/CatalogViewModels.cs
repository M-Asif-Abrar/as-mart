// Models/ViewModels/CatalogViewModels.cs
using System;
using System.Collections.Generic;
using AsMart.Web.Models.Entities;

namespace AsMart.Web.Models.ViewModels
{
    public class CatalogIndexViewModel
    {
        public List<Product> Products { get; set; } = new();

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public List<Category> AllCategories { get; set; } = new();
        public Dictionary<int, int> CategoryCounts { get; set; } = new();

        public List<int> SelectedCategoryIds { get; set; } = new();
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public decimal? MinRating { get; set; }
        public decimal? MaxRating { get; set; }

        public List<string> AllBrands { get; set; } = new();
        public List<BrandFilterOptionVm> BrandOptions { get; set; } = new();
        public string? Brand { get; set; }

        public string CategoriesCsv => SelectedCategoryIds.Count == 0
            ? string.Empty
            : string.Join(",", SelectedCategoryIds);
    }

    public class CatalogCategoryViewModel
    {
        public Category Category { get; set; } = null!;
        public List<Product> Products { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class CatalogSearchViewModel
    {
        public string Query { get; set; } = string.Empty;
        public List<Product> Products { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class AdminCategoryGroupVm
    {
        public int ParentId { get; set; }
        public string ParentName { get; set; } = "";
        public List<AdminCategoryItemVm> Children { get; set; } = new();
    }

    public class AdminCategoryItemVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class BrandFilterOptionVm
    {
        public string Name { get; set; } = "";
        public List<BrandFilterCategoryVm> Categories { get; set; } = new();
    }

    public class BrandFilterCategoryVm
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }
}