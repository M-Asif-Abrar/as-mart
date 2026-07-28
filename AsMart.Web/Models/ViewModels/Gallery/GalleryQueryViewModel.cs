using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.ViewModels.Gallery
{
    public sealed class GalleryQueryViewModel
    {
        private const int DefaultPageSize = 60;
        private const int MaximumPageSize = 200;

        /// <summary>
        /// Searches Product Title, ASIN, Brand, Slug,
        /// ShortDescription and category name.
        /// </summary>
        [Display(Name = "Search")]
        public string? SearchTerm { get; set; }

        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Display(Name = "Brand")]
        public string? Brand { get; set; }

        [Display(Name = "Image Type")]
        public GalleryImageType? ImageType { get; set; }

        [Display(Name = "Product Status")]
        public bool? IsActive { get; set; } = true;

        [Display(Name = "Featured Only")]
        public bool FeaturedOnly { get; set; }

        [Display(Name = "Deal Products Only")]
        public bool DealsOnly { get; set; }

        [Display(Name = "Products With Images Only")]
        public bool HasImagesOnly { get; set; } = true;

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, MaximumPageSize)]
        public int PageSize { get; set; } = DefaultPageSize;

        public string SortBy { get; set; } = "updated-desc";

        public void Normalize()
        {
            SearchTerm = NormalizeText(SearchTerm);
            Brand = NormalizeText(Brand);
            SortBy = NormalizeSortBy(SortBy);

            if (Page < 1)
            {
                Page = 1;
            }

            if (PageSize < 1)
            {
                PageSize = DefaultPageSize;
            }

            if (PageSize > MaximumPageSize)
            {
                PageSize = MaximumPageSize;
            }
        }

        private static string? NormalizeText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static string NormalizeSortBy(string? sortBy)
        {
            return sortBy?.Trim().ToLowerInvariant() switch
            {
                "created-desc" => "created-desc",
                "created-asc" => "created-asc",
                "updated-desc" => "updated-desc",
                "updated-asc" => "updated-asc",
                "title-asc" => "title-asc",
                "title-desc" => "title-desc",
                "rating-desc" => "rating-desc",
                "clicks-desc" => "clicks-desc",
                "product-id-desc" => "product-id-desc",
                "product-id-asc" => "product-id-asc",
                _ => "updated-desc"
            };
        }
    }
}