using System;
using System.Collections.Generic;

namespace AsMart.Web.Models.ViewModels.Gallery
{
    public sealed class GalleryIndexViewModel
    {
        public GalleryQueryViewModel Query { get; init; } = new();

        public IReadOnlyList<GalleryImageItemViewModel> Images { get; init; } =
            Array.Empty<GalleryImageItemViewModel>();

        public IReadOnlyList<GalleryFilterCategoryViewModel> AvailableCategories
        { get; init; } =
            Array.Empty<GalleryFilterCategoryViewModel>();

        public IReadOnlyList<string> AvailableBrands { get; init; } =
            Array.Empty<string>();

        /// <summary>
        /// Total number of individual image rows after filters.
        /// A single product can produce multiple image rows.
        /// </summary>
        public int TotalImages { get; init; }

        /// <summary>
        /// Total distinct products represented by the filtered image rows.
        /// </summary>
        public int TotalProducts { get; init; }

        public int MainImageCount { get; init; }

        public int AdditionalImageCount { get; init; }

        public int CurrentPage => Query.Page;

        public int PageSize => Query.PageSize;

        public int TotalPages =>
            TotalImages <= 0
                ? 0
                : (int)Math.Ceiling(TotalImages / (double)PageSize);

        public bool HasPreviousPage =>
            CurrentPage > 1;

        public bool HasNextPage =>
            CurrentPage < TotalPages;

        public int FirstRecordNumber =>
            TotalImages == 0
                ? 0
                : ((CurrentPage - 1) * PageSize) + 1;

        public int LastRecordNumber =>
            Math.Min(CurrentPage * PageSize, TotalImages);
    }

    public sealed class GalleryFilterCategoryViewModel
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Slug { get; init; } = string.Empty;

        public int? ParentCategoryId { get; init; }

        public string DisplayName { get; init; } = string.Empty;
    }
}