using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using AsMart.Web.Models.ViewModels.Gallery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AsMart.Web.Services.Gallery
{
    public sealed class GalleryService : IGalleryService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<GalleryService> _logger;

        public GalleryService(
            ApplicationDbContext db,
            ILogger<GalleryService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<GalleryIndexViewModel> GetGalleryAsync(
            GalleryQueryViewModel query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            query.Normalize();

            /*
             * Start with the Products table.
             *
             * AsNoTracking is used because Gallery is read-only.
             * It avoids unnecessary EF Core change-tracking overhead.
             */
            IQueryable<Product> productQuery = _db.Products
                .AsNoTracking();

            productQuery = ApplyProductFilters(productQuery, query);

            /*
             * AdditionalImagesJson cannot be expanded inside SQL.
             *
             * First retrieve the filtered product records from SQL Server.
             * Then convert MainImageUrl and AdditionalImagesJson into
             * individual gallery image records in memory.
             */
            var products = await productQuery
                .Select(product => new GalleryProductRecord
                {
                    Id = product.Id,
                    ASIN = product.ASIN,
                    Title = product.Title,
                    Slug = product.Slug,
                    ShortDescription = product.ShortDescription,
                    Description = product.Description,
                    Brand = product.Brand,
                    Price = product.Price,
                    ListPrice = product.ListPrice,
                    Currency = product.Currency,
                    Rating = product.Rating,
                    RatingCount = product.RatingCount,
                    MainImageUrl = product.MainImageUrl,
                    AdditionalImagesJson = product.AdditionalImagesJson,
                    AffiliateUrlOverride = product.AffiliateUrlOverride,
                    IsFeatured = product.IsFeatured,
                    IsActive = product.IsActive,
                    IsDealOfTheDay = product.IsDealOfTheDay,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    LastSyncedAt = product.LastSyncedAt,
                    ClickCount = product.ClickCount,

                    Categories = product.ProductCategories
                        .Where(productCategory =>
                            productCategory.Category != null)
                        .Select(productCategory =>
                            new GalleryCategoryRecord
                            {
                                Id = productCategory.Category.Id,
                                Name = productCategory.Category.Name,
                                Slug = productCategory.Category.Slug,

                                ParentCategoryId =
                                    productCategory.Category.ParentCategoryId,

                                /*
                                 * ParentCategoryName remains null because
                                 * your Category entity may not have a
                                 * ParentCategory navigation property.
                                 *
                                 * This prevents compilation errors.
                                 */
                                ParentCategoryName = null
                            })
                        .OrderBy(category => category.Name)
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            /*
             * One product may generate several Gallery records:
             *
             * 1 record from MainImageUrl
             * Multiple records from AdditionalImagesJson
             */
            var allImages = new List<GalleryImageItemViewModel>();

            foreach (var product in products)
            {
                AddProductImages(
                    destination: allImages,
                    product: product,
                    requiredImageType: query.ImageType);
            }

            /*
             * Apply sorting after expanding product records into
             * individual image records.
             */
            var sortedImages = ApplyImageSorting(
                    images: allImages,
                    sortBy: query.SortBy)
                .ToList();

            var totalImages = sortedImages.Count;

            var totalProducts = sortedImages
                .Select(image => image.ProductId)
                .Distinct()
                .Count();

            var mainImageCount = sortedImages.Count(image =>
                image.ImageType == GalleryImageType.Main);

            var additionalImageCount = sortedImages.Count(image =>
                image.ImageType == GalleryImageType.Additional);

            /*
             * Calculate total pages using individual image records,
             * not product records.
             */
            var totalPages = totalImages == 0
                ? 0
                : (int)Math.Ceiling(
                    totalImages / (double)query.PageSize);

            /*
             * If the user requests a page that no longer exists,
             * move them to the final available page.
             */
            if (totalPages > 0 && query.Page > totalPages)
            {
                query.Page = totalPages;
            }

            if (query.Page < 1)
            {
                query.Page = 1;
            }

            var pagedImages = sortedImages
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            /*
             * These lists populate the category and brand filters
             * displayed above the Gallery.
             */
            var availableCategories =
                await GetAvailableCategoriesAsync(cancellationToken);

            var availableBrands =
                await GetAvailableBrandsAsync(cancellationToken);

            return new GalleryIndexViewModel
            {
                Query = query,
                Images = pagedImages,
                AvailableCategories = availableCategories,
                AvailableBrands = availableBrands,
                TotalImages = totalImages,
                TotalProducts = totalProducts,
                MainImageCount = mainImageCount,
                AdditionalImageCount = additionalImageCount
            };
        }

        private static IQueryable<Product> ApplyProductFilters(
            IQueryable<Product> products,
            GalleryQueryViewModel query)
        {
            /*
             * Default Gallery behavior is normally active products only
             * because GalleryQueryViewModel.IsActive defaults to true.
             */
            if (query.IsActive.HasValue)
            {
                products = products.Where(product =>
                    product.IsActive == query.IsActive.Value);
            }

            if (query.FeaturedOnly)
            {
                products = products.Where(product =>
                    product.IsFeatured);
            }

            if (query.DealsOnly)
            {
                products = products.Where(product =>
                    product.IsDealOfTheDay);
            }

            if (query.HasImagesOnly)
            {
                products = products.Where(product =>
                    (
                        product.MainImageUrl != null &&
                        product.MainImageUrl != string.Empty
                    )
                    ||
                    (
                        product.AdditionalImagesJson != null &&
                        product.AdditionalImagesJson != string.Empty
                    ));
            }

            /*
             * This filters products through the ProductCategories
             * join table.
             */
            if (query.CategoryId.HasValue)
            {
                var categoryId = query.CategoryId.Value;

                products = products.Where(product =>
                    product.ProductCategories.Any(productCategory =>
                        productCategory.CategoryId == categoryId));
            }

            if (!string.IsNullOrWhiteSpace(query.Brand))
            {
                var selectedBrand = query.Brand.Trim();

                products = products.Where(product =>
                    product.Brand != null &&
                    product.Brand == selectedBrand);
            }

            /*
             * Search across product and category fields.
             */
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var escapedSearchTerm =
                    EscapeLikePattern(query.SearchTerm.Trim());

                var searchPattern = $"%{escapedSearchTerm}%";

                products = products.Where(product =>
                    EF.Functions.Like(
                        product.Title,
                        searchPattern,
                        @"\")
                    ||
                    EF.Functions.Like(
                        product.ASIN,
                        searchPattern,
                        @"\")
                    ||
                    EF.Functions.Like(
                        product.Slug,
                        searchPattern,
                        @"\")
                    ||
                    (
                        product.Brand != null &&
                        EF.Functions.Like(
                            product.Brand,
                            searchPattern,
                            @"\")
                    )
                    ||
                    (
                        product.ShortDescription != null &&
                        EF.Functions.Like(
                            product.ShortDescription,
                            searchPattern,
                            @"\")
                    )
                    ||
                    product.ProductCategories.Any(productCategory =>
                        productCategory.Category != null &&
                        EF.Functions.Like(
                            productCategory.Category.Name,
                            searchPattern,
                            @"\"))
                );
            }

            return products;
        }

        private void AddProductImages(
            ICollection<GalleryImageItemViewModel> destination,
            GalleryProductRecord product,
            GalleryImageType? requiredImageType)
        {
            var categories = product.Categories
                .Select(category =>
                    new GalleryCategoryViewModel
                    {
                        Id = category.Id,
                        Name = category.Name,
                        Slug = category.Slug,
                        ParentCategoryId = category.ParentCategoryId,
                        ParentCategoryName = category.ParentCategoryName
                    })
                .ToList();

            /*
             * This prevents duplicate image URLs for the same product.
             *
             * For example, Amazon data may include MainImageUrl again
             * inside AdditionalImagesJson.
             */
            var usedImageUrls = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            var imageNumber = 0;

            /*
             * Add MainImageUrl.
             */
            if (requiredImageType is null or GalleryImageType.Main)
            {
                var mainImageUrl =
                    NormalizeImageUrl(product.MainImageUrl);

                if (mainImageUrl is not null &&
                    usedImageUrls.Add(mainImageUrl))
                {
                    imageNumber++;

                    destination.Add(
                        CreateGalleryImage(
                            product: product,
                            categories: categories,
                            imageUrl: mainImageUrl,
                            imageType: GalleryImageType.Main,
                            imageNumber: imageNumber,
                            additionalImageIndex: null));
                }
            }

            /*
             * Add every valid image from AdditionalImagesJson.
             */
            if (requiredImageType is null or GalleryImageType.Additional)
            {
                var additionalImages =
                    ParseAdditionalImageUrls(
                        productId: product.Id,
                        additionalImagesJson:
                            product.AdditionalImagesJson);

                for (var index = 0;
                     index < additionalImages.Count;
                     index++)
                {
                    var additionalImageUrl =
                        additionalImages[index];

                    if (!usedImageUrls.Add(additionalImageUrl))
                    {
                        continue;
                    }

                    imageNumber++;

                    destination.Add(
                        CreateGalleryImage(
                            product: product,
                            categories: categories,
                            imageUrl: additionalImageUrl,
                            imageType:
                                GalleryImageType.Additional,
                            imageNumber: imageNumber,
                            additionalImageIndex: index));
                }
            }
        }

        private static GalleryImageItemViewModel CreateGalleryImage(
            GalleryProductRecord product,
            IReadOnlyList<GalleryCategoryViewModel> categories,
            string imageUrl,
            GalleryImageType imageType,
            int imageNumber,
            int? additionalImageIndex)
        {
            return new GalleryImageItemViewModel
            {
                GalleryItemId = BuildGalleryItemId(
                    productId: product.Id,
                    imageType: imageType,
                    additionalImageIndex:
                        additionalImageIndex),

                ImageUrl = imageUrl,
                ImageType = imageType,
                ImageNumber = imageNumber,

                AdditionalImageIndex =
                    additionalImageIndex,

                ProductId = product.Id,
                ASIN = product.ASIN,
                Title = product.Title,
                Slug = product.Slug,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                Brand = product.Brand,
                Price = product.Price,
                ListPrice = product.ListPrice,
                Currency = product.Currency,
                Rating = product.Rating,
                RatingCount = product.RatingCount,
                MainImageUrl = product.MainImageUrl,

                AdditionalImagesJson =
                    product.AdditionalImagesJson,

                AffiliateUrlOverride =
                    product.AffiliateUrlOverride,

                IsFeatured = product.IsFeatured,
                IsActive = product.IsActive,
                IsDealOfTheDay = product.IsDealOfTheDay,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                LastSyncedAt = product.LastSyncedAt,
                ClickCount = product.ClickCount,
                Categories = categories
            };
        }

        private IReadOnlyList<string> ParseAdditionalImageUrls(
            int productId,
            string? additionalImagesJson)
        {
            if (string.IsNullOrWhiteSpace(
                    additionalImagesJson))
            {
                return Array.Empty<string>();
            }

            try
            {
                using var document =
                    JsonDocument.Parse(
                        additionalImagesJson);

                var discoveredUrls =
                    new List<string>();

                ExtractImageUrls(
                    document.RootElement,
                    discoveredUrls);

                return discoveredUrls
                    .Select(NormalizeImageUrl)
                    .Where(imageUrl =>
                        imageUrl is not null)
                    .Select(imageUrl => imageUrl!)
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (JsonException exception)
            {
                /*
                 * Invalid JSON for one product must not crash
                 * the complete Gallery page.
                 */
                _logger.LogWarning(
                    exception,
                    "Invalid AdditionalImagesJson found for ProductId {ProductId}.",
                    productId);

                /*
                 * Try parsing older records containing comma,
                 * pipe, semicolon or line-separated URLs.
                 */
                return ParseFallbackDelimitedUrls(
                    additionalImagesJson);
            }
        }

        private static void ExtractImageUrls(
            JsonElement element,
            ICollection<string> imageUrls)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    {
                        var value = element.GetString();

                        if (LooksLikeImageUrl(value))
                        {
                            imageUrls.Add(value!);
                        }

                        break;
                    }

                case JsonValueKind.Array:
                    {
                        foreach (var childElement
                                 in element.EnumerateArray())
                        {
                            ExtractImageUrls(
                                childElement,
                                imageUrls);
                        }

                        break;
                    }

                case JsonValueKind.Object:
                    {
                        /*
                         * This supports different JSON structures:
                         *
                         * ["url1", "url2"]
                         *
                         * [
                         *   { "url": "url1" },
                         *   { "imageUrl": "url2" }
                         * ]
                         *
                         * { "images": ["url1", "url2"] }
                         *
                         * { "large": { "url": "url1" } }
                         */
                        foreach (var property
                                 in element.EnumerateObject())
                        {
                            ExtractImageUrls(
                                property.Value,
                                imageUrls);
                        }

                        break;
                    }
            }
        }

        private static IReadOnlyList<string>
            ParseFallbackDelimitedUrls(string value)
        {
            char[] separators =
            {
                ',',
                '|',
                ';',
                '\r',
                '\n'
            };

            return value
                .Split(
                    separators,
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Select(NormalizeImageUrl)
                .Where(imageUrl =>
                    imageUrl is not null)
                .Select(imageUrl => imageUrl!)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<GalleryImageItemViewModel>
            ApplyImageSorting(
                IEnumerable<GalleryImageItemViewModel> images,
                string sortBy)
        {
            return sortBy switch
            {
                "created-desc" => images
                    .OrderByDescending(image =>
                        image.CreatedAt)
                    .ThenByDescending(image =>
                        image.ProductId)
                    .ThenBy(image =>
                        image.ImageNumber),

                "created-asc" => images
                    .OrderBy(image =>
                        image.CreatedAt)
                    .ThenBy(image =>
                        image.ProductId)
                    .ThenBy(image =>
                        image.ImageNumber),

                "updated-asc" => images
                    .OrderBy(image =>
                        image.UpdatedAt)
                    .ThenBy(image =>
                        image.ProductId)
                    .ThenBy(image =>
                        image.ImageNumber),

                "title-asc" => images
                    .OrderBy(image =>
                        image.Title)
                    .ThenBy(image =>
                        image.ProductId)
                    .ThenBy(image =>
                        image.ImageNumber),

                "title-desc" => images
                    .OrderByDescending(image =>
                        image.Title)
                    .ThenByDescending(image =>
                        image.ProductId)
                    .ThenBy(image =>
                        image.ImageNumber),

                "rating-desc" => images
                    .OrderByDescending(image =>
                        image.Rating ?? 0)
                    .ThenByDescending(image =>
                        image.RatingCount ?? 0)
                    .ThenBy(image =>
                        image.Title)
                    .ThenBy(image =>
                        image.ImageNumber),

                "clicks-desc" => images
                    .OrderByDescending(image =>
                        image.ClickCount)
                    .ThenByDescending(image =>
                        image.Rating ?? 0)
                    .ThenBy(image =>
                        image.Title)
                    .ThenBy(image =>
                        image.ImageNumber),

                "product-id-asc" => images
                    .OrderBy(image =>
                        image.ProductId)
                    .ThenBy(image =>
                        image.ImageNumber),

                "product-id-desc" => images
                    .OrderByDescending(image =>
                        image.ProductId)
                    .ThenBy(image =>
                        image.ImageNumber),

                /*
                 * Default:
                 * Recently updated products first.
                 */
                _ => images
                    .OrderByDescending(image =>
                        image.UpdatedAt)
                    .ThenByDescending(image =>
                        image.ProductId)
                    .ThenBy(image =>
                        image.ImageNumber)
            };
        }

        private async Task<
            IReadOnlyList<GalleryFilterCategoryViewModel>>
            GetAvailableCategoriesAsync(
                CancellationToken cancellationToken)
        {
            /*
             * Categories automatically appear in the Gallery category row
             * when they contain at least one active product with an image.
             *
             * Therefore, no Gallery code change is required when you add
             * a new category and assign products to it.
             */
            return await _db.Categories
                .AsNoTracking()
                .Where(category =>
                    category.ProductCategories.Any(
                        productCategory =>
                            productCategory.Product.IsActive
                            &&
                            (
                                (
                                    productCategory.Product
                                        .MainImageUrl != null
                                    &&
                                    productCategory.Product
                                        .MainImageUrl != string.Empty
                                )
                                ||
                                (
                                    productCategory.Product
                                        .AdditionalImagesJson != null
                                    &&
                                    productCategory.Product
                                        .AdditionalImagesJson != string.Empty
                                )
                            )))
                .OrderBy(category =>
                    category.Name)
                .Select(category =>
                    new GalleryFilterCategoryViewModel
                    {
                        Id = category.Id,
                        Name = category.Name,
                        Slug = category.Slug,

                        ParentCategoryId =
                            category.ParentCategoryId,

                        /*
                         * No ParentCategory navigation is needed.
                         */
                        DisplayName = category.Name
                    })
                .ToListAsync(cancellationToken);
        }

        private async Task<IReadOnlyList<string>>
            GetAvailableBrandsAsync(
                CancellationToken cancellationToken)
        {
            return await _db.Products
                .AsNoTracking()
                .Where(product =>
                    product.IsActive
                    &&
                    product.Brand != null
                    &&
                    product.Brand != string.Empty
                    &&
                    (
                        (
                            product.MainImageUrl != null
                            &&
                            product.MainImageUrl != string.Empty
                        )
                        ||
                        (
                            product.AdditionalImagesJson != null
                            &&
                            product.AdditionalImagesJson != string.Empty
                        )
                    ))
                .Select(product =>
                    product.Brand!)
                .Distinct()
                .OrderBy(brand =>
                    brand)
                .ToListAsync(cancellationToken);
        }

        private static string BuildGalleryItemId(
            int productId,
            GalleryImageType imageType,
            int? additionalImageIndex)
        {
            return imageType == GalleryImageType.Main
                ? $"product-{productId}-main"
                : $"product-{productId}-additional-{additionalImageIndex ?? 0}";
        }

        private static string? NormalizeImageUrl(
            string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return null;
            }

            var normalizedUrl = imageUrl
                .Trim()
                .Trim('"', '\'');

            /*
             * Gallery images must use absolute HTTP or HTTPS URLs.
             */
            if (!Uri.TryCreate(
                    normalizedUrl,
                    UriKind.Absolute,
                    out var uri))
            {
                return null;
            }

            if (uri.Scheme != Uri.UriSchemeHttp &&
                uri.Scheme != Uri.UriSchemeHttps)
            {
                return null;
            }

            return uri.AbsoluteUri;
        }

        private static bool LooksLikeImageUrl(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return Uri.TryCreate(
                       value.Trim(),
                       UriKind.Absolute,
                       out var uri)
                   &&
                   (
                       uri.Scheme == Uri.UriSchemeHttp
                       ||
                       uri.Scheme == Uri.UriSchemeHttps
                   );
        }

        private static string EscapeLikePattern(
            string value)
        {
            /*
             * Escape SQL LIKE wildcard characters so user input
             * is handled as normal search text.
             */
            return value
                .Replace(
                    @"\",
                    @"\\",
                    StringComparison.Ordinal)
                .Replace(
                    "%",
                    @"\%",
                    StringComparison.Ordinal)
                .Replace(
                    "_",
                    @"\_",
                    StringComparison.Ordinal)
                .Replace(
                    "[",
                    @"\[",
                    StringComparison.Ordinal);
        }

        /*
         * Internal records used only for database projection.
         *
         * EF entities are not directly passed to the Razor view.
         */
        private sealed class GalleryProductRecord
        {
            public int Id { get; init; }

            public string ASIN { get; init; } =
                string.Empty;

            public string Title { get; init; } =
                string.Empty;

            public string Slug { get; init; } =
                string.Empty;

            public string? ShortDescription { get; init; }

            public string? Description { get; init; }

            public string? Brand { get; init; }

            public decimal? Price { get; init; }

            public decimal? ListPrice { get; init; }

            public string Currency { get; init; } =
                "USD";

            public decimal? Rating { get; init; }

            public int? RatingCount { get; init; }

            public string? MainImageUrl { get; init; }

            public string? AdditionalImagesJson
            {
                get;
                init;
            }

            public string? AffiliateUrlOverride
            {
                get;
                init;
            }

            public bool IsFeatured { get; init; }

            public bool IsActive { get; init; }

            public bool IsDealOfTheDay { get; init; }

            public DateTime CreatedAt { get; init; }

            public DateTime UpdatedAt { get; init; }

            public DateTime? LastSyncedAt { get; init; }

            public int ClickCount { get; init; }

            public List<GalleryCategoryRecord> Categories
            {
                get;
                init;
            } = new();
        }

        private sealed class GalleryCategoryRecord
        {
            public int Id { get; init; }

            public string Name { get; init; } =
                string.Empty;

            public string Slug { get; init; } =
                string.Empty;

            public int? ParentCategoryId { get; init; }

            public string? ParentCategoryName { get; init; }
        }
    }
}