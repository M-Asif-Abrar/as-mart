using System;
using System.Collections.Generic;
using System.Linq;

namespace AsMart.Web.Models.ViewModels.Gallery
{
    public sealed class GalleryImageItemViewModel
    {
        // ------------------------------------------------------------
        // Image information
        // ------------------------------------------------------------

        /// <summary>
        /// Unique row identifier generated for the Gallery result.
        /// This is not stored in the database.
        /// </summary>
        public string GalleryItemId { get; init; } = string.Empty;

        /// <summary>
        /// Direct URL of the image.
        /// </summary>
        public string ImageUrl { get; init; } = string.Empty;

        /// <summary>
        /// Indicates whether the image comes from MainImageUrl
        /// or AdditionalImagesJson.
        /// </summary>
        public GalleryImageType ImageType { get; init; }

        /// <summary>
        /// Position of the image inside the product image collection.
        /// Main image is normally 1.
        /// </summary>
        public int ImageNumber { get; init; }

        /// <summary>
        /// Original position inside AdditionalImagesJson.
        /// Null for the main image.
        /// </summary>
        public int? AdditionalImageIndex { get; init; }

        public bool IsMainImage =>
            ImageType == GalleryImageType.Main;

        public string ImageTypeText =>
            IsMainImage ? "Main" : "Additional";

        // ------------------------------------------------------------
        // Product information
        // ------------------------------------------------------------

        public int ProductId { get; init; }

        public string ASIN { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string Slug { get; init; } = string.Empty;

        public string? ShortDescription { get; init; }

        public string? Description { get; init; }

        public string? Brand { get; init; }

        public decimal? Price { get; init; }

        public decimal? ListPrice { get; init; }

        public string Currency { get; init; } = "USD";

        public decimal? Rating { get; init; }

        public int? RatingCount { get; init; }

        public string? MainImageUrl { get; init; }

        public string? AdditionalImagesJson { get; init; }

        public string? AffiliateUrlOverride { get; init; }

        public bool IsFeatured { get; init; }

        public bool IsActive { get; init; }

        public bool IsDealOfTheDay { get; init; }

        public DateTime CreatedAt { get; init; }

        public DateTime UpdatedAt { get; init; }

        public DateTime? LastSyncedAt { get; init; }

        public int ClickCount { get; init; }

        // ------------------------------------------------------------
        // Category information
        // ------------------------------------------------------------

        public IReadOnlyList<GalleryCategoryViewModel> Categories { get; init; } =
            Array.Empty<GalleryCategoryViewModel>();

        public string CategoryNames =>
            Categories.Count == 0
                ? "Uncategorized"
                : string.Join(", ", Categories
                    .Select(category => category.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase));

        public int? PrimaryCategoryId =>
            Categories
                .OrderByDescending(category => category.ParentCategoryId.HasValue)
                .ThenBy(category => category.Name)
                .Select(category => (int?)category.Id)
                .FirstOrDefault();

        public string PrimaryCategoryName =>
            Categories
                .OrderByDescending(category => category.ParentCategoryId.HasValue)
                .ThenBy(category => category.Name)
                .Select(category => category.Name)
                .FirstOrDefault()
            ?? "Uncategorized";
    }

    public sealed class GalleryCategoryViewModel
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Slug { get; init; } = string.Empty;

        public int? ParentCategoryId { get; init; }

        public string? ParentCategoryName { get; init; }
    }
}