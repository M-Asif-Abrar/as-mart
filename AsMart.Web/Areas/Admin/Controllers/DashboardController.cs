using AsMart.Web.Data;
using AsMart.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public sealed class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly TimeProvider _timeProvider;

        public DashboardController(
            ApplicationDbContext db,
            TimeProvider timeProvider)
        {
            _db = db;
            _timeProvider = timeProvider;
        }

        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var utcToday = nowUtc.Date;
            var tomorrowUtc = utcToday.AddDays(1);
            var last7 = utcToday.AddDays(-6);
            var last30 = utcToday.AddDays(-29);

            var totalApiRequestsLast30Days = await _db.ApiUsageLogs
                .CountAsync(
                    x => x.CreatedAt >= last30 &&
                         x.CreatedAt < tomorrowUtc,
                    cancellationToken);

            var apiErrorsLast30Days = await _db.ApiUsageLogs
                .CountAsync(
                    x => x.CreatedAt >= last30 &&
                         x.CreatedAt < tomorrowUtc &&
                         x.StatusCode >= 400,
                    cancellationToken);

            var averageRating = await _db.Products
                .Where(x => x.Rating > 0)
                .Select(x => (decimal?)x.Rating)
                .AverageAsync(cancellationToken) ?? 0m;

            var vm = new AdminDashboardViewModel
            {
                TotalUsers = await _db.Users.CountAsync(cancellationToken),

                TotalProducts = await _db.Products
                    .CountAsync(cancellationToken),

                ActiveProducts = await _db.Products
                    .CountAsync(x => x.IsActive, cancellationToken),

                FeaturedProducts = await _db.Products
                    .CountAsync(x => x.IsFeatured, cancellationToken),

                DealProducts = await _db.Products
                    .CountAsync(x => x.IsDealOfTheDay, cancellationToken),

                TotalCategories = await _db.Categories
                    .CountAsync(cancellationToken),

                TotalBlogPosts = await _db.BlogPosts
                    .CountAsync(cancellationToken),

                VisibleBlogPosts = await _db.BlogPosts
                        .CountAsync(x => x.IsPublished, cancellationToken),

                TotalCollections = await _db.Collections
                    .CountAsync(cancellationToken),

                TotalClickLogs = await _db.ClickLogs
                    .CountAsync(cancellationToken),

                TotalUserProductStatuses =
                    await _db.UserProductStatuses
                        .CountAsync(cancellationToken),

                ClicksToday = await _db.ClickLogs
                    .CountAsync(
                        x => x.ClickedAt >= utcToday &&
                             x.ClickedAt < tomorrowUtc,
                        cancellationToken),

                ClicksLast7Days = await _db.ClickLogs
                    .CountAsync(
                        x => x.ClickedAt >= last7 &&
                             x.ClickedAt < tomorrowUtc,
                        cancellationToken),

                ClicksLast30Days = await _db.ClickLogs
                    .CountAsync(
                        x => x.ClickedAt >= last30 &&
                             x.ClickedAt < tomorrowUtc,
                        cancellationToken),

                SocialClicksLast30Days = await _db.ClickLogs
                    .CountAsync(
                        x => x.ClickedAt >= last30 &&
                             x.ClickedAt < tomorrowUtc &&
                             x.IsSocialTraffic,
                        cancellationToken),

                FacebookClicksLast30Days = await _db.ClickLogs
                    .CountAsync(
                        x => x.ClickedAt >= last30 &&
                             x.ClickedAt < tomorrowUtc &&
                             x.IsFacebookTraffic,
                        cancellationToken),

                AverageProductRating = Math.Round(averageRating, 2),

                TotalApiClients = await _db.ApiClients
                    .CountAsync(cancellationToken),

                ActiveApiClients = await _db.ApiClients
                    .CountAsync(
                        x => x.IsActive &&
                             x.RevokedAt == null &&
                             (x.ExpiresAt == null ||
                              x.ExpiresAt > nowUtc),
                        cancellationToken),

                ApiRequestsToday = await _db.ApiUsageLogs
                    .CountAsync(
                        x => x.CreatedAt >= utcToday &&
                             x.CreatedAt < tomorrowUtc,
                        cancellationToken),

                ApiRequestsLast7Days = await _db.ApiUsageLogs
                    .CountAsync(
                        x => x.CreatedAt >= last7 &&
                             x.CreatedAt < tomorrowUtc,
                        cancellationToken),

                ApiRequestsLast30Days = totalApiRequestsLast30Days,
                ApiErrorsLast30Days = apiErrorsLast30Days,

                ApiSuccessRateLast30Days =
                    totalApiRequestsLast30Days == 0
                        ? 100
                        : Math.Round(
                            (totalApiRequestsLast30Days -
                             apiErrorsLast30Days) * 100d /
                            totalApiRequestsLast30Days,
                            1),

                ActiveRefreshTokens = await _db.RefreshTokens
                    .CountAsync(
                        x => x.RevokedAtUtc == null &&
                             x.ExpiresAtUtc > nowUtc,
                        cancellationToken),

                RevokedRefreshTokens = await _db.RefreshTokens
                    .CountAsync(
                        x => x.RevokedAtUtc != null,
                        cancellationToken),

                ExpiredRefreshTokens = await _db.RefreshTokens
                    .CountAsync(
                        x => x.ExpiresAtUtc <= nowUtc,
                        cancellationToken)
            };

            vm.ProductsPerCategory = await _db.Categories
                .AsNoTracking()
                .Select(c =>
                    new AdminDashboardViewModel.CategoryProductsItem
                    {
                        CategoryName = c.Name,
                        ProductCount = c.ProductCategories.Count()
                    })
                .OrderByDescending(x => x.ProductCount)
                .ThenBy(x => x.CategoryName)
                .Take(10)
                .ToListAsync(cancellationToken);

            vm.ProductsPerCollection = await _db.Collections
                .AsNoTracking()
                .Select(c =>
                    new AdminDashboardViewModel.CollectionProductsItem
                    {
                        CollectionName = c.Name,
                        ProductCount = c.CollectionProducts.Count()
                    })
                .OrderByDescending(x => x.ProductCount)
                .ThenBy(x => x.CollectionName)
                .Take(10)
                .ToListAsync(cancellationToken);

            var dailyClickCounts = await _db.ClickLogs
                .AsNoTracking()
                .Where(x => x.ClickedAt >= last30 &&
                            x.ClickedAt < tomorrowUtc)
                .GroupBy(x => x.ClickedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(
                    x => x.Date,
                    x => x.Count,
                    cancellationToken);

            vm.DailyClicks = Enumerable
                .Range(0, 30)
                .Select(index =>
                {
                    var date = last30.AddDays(index);

                    return new AdminDashboardViewModel.DailyMetricItem
                    {
                        Date = date,
                        Count = dailyClickCounts.GetValueOrDefault(date)
                    };
                })
                .ToList();

            var dailyApiCounts = await _db.ApiUsageLogs
                .AsNoTracking()
                .Where(x => x.CreatedAt >= last30 &&
                            x.CreatedAt < tomorrowUtc)
                .GroupBy(x => x.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(
                    x => x.Date,
                    x => x.Count,
                    cancellationToken);

            vm.DailyApiRequests = Enumerable
                .Range(0, 30)
                .Select(index =>
                {
                    var date = last30.AddDays(index);

                    return new AdminDashboardViewModel.DailyMetricItem
                    {
                        Date = date,
                        Count = dailyApiCounts.GetValueOrDefault(date)
                    };
                })
                .ToList();

            vm.TrafficSources = await _db.ClickLogs
                .AsNoTracking()
                .Where(x => x.ClickedAt >= last30 &&
                            x.ClickedAt < tomorrowUtc)
                .GroupBy(x =>
                    string.IsNullOrEmpty(x.UtmSource)
                        ? "Direct / Unknown"
                        : x.UtmSource)
                .Select(g =>
                    new AdminDashboardViewModel.NamedCountItem
                    {
                        Name = g.Key!,
                        Count = g.Count()
                    })
                .OrderByDescending(x => x.Count)
                .Take(8)
                .ToListAsync(cancellationToken);

            vm.ApiStatusGroups = await _db.ApiUsageLogs
                .AsNoTracking()
                .Where(x => x.CreatedAt >= last30 &&
                            x.CreatedAt < tomorrowUtc)
                .GroupBy(x =>
                    x.StatusCode >= 500 ? "5xx Server Error" :
                    x.StatusCode >= 400 ? "4xx Client Error" :
                    x.StatusCode >= 300 ? "3xx Redirect" :
                    x.StatusCode >= 200 ? "2xx Success" :
                    "Other")
                .Select(g =>
                    new AdminDashboardViewModel.NamedCountItem
                    {
                        Name = g.Key,
                        Count = g.Count()
                    })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

            vm.TopProductsByClicks = await _db.ClickLogs
                .AsNoTracking()
                .Where(x => x.ClickedAt >= last30 &&
                            x.ClickedAt < tomorrowUtc &&
                            x.ProductId != 0)
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Clicks = g.Count(),
                    LastClickedAt = g.Max(x => x.ClickedAt)
                })
                .OrderByDescending(x => x.Clicks)
                .Take(10)
                .Join(
                    _db.Products.AsNoTracking(),
                    click => click.ProductId,
                    product => product.Id,
                    (click, product) => new
                    {
                        click,
                        product
                    })
                .Select(x =>
                    new AdminDashboardViewModel.TopProductClicksItem
                    {
                        ProductId = x.product.Id,
                        Slug = x.product.Slug,
                        Title = x.product.Title,
                        CategoryName = x.product.ProductCategories
                            .Select(pc => pc.Category!.Name)
                            .FirstOrDefault(),
                        Clicks = x.click.Clicks,
                        LastClickedAt = x.click.LastClickedAt
                    })
                .ToListAsync(cancellationToken);

            vm.TopCategoriesByClicks = await _db.ClickLogs
                .AsNoTracking()
                .Where(x => x.ClickedAt >= last30 &&
                            x.ClickedAt < tomorrowUtc)
                .Join(
                    _db.ProductCategories.AsNoTracking(),
                    click => click.ProductId,
                    productCategory => productCategory.ProductId,
                    (click, productCategory) =>
                        productCategory.CategoryId)
                .Join(
                    _db.Categories.AsNoTracking(),
                    categoryId => categoryId,
                    category => category.Id,
                    (categoryId, category) => category.Name)
                .GroupBy(x => x)
                .Select(g =>
                    new AdminDashboardViewModel.TopCategoryClicksItem
                    {
                        CategoryName = g.Key,
                        Clicks = g.Count()
                    })
                .OrderByDescending(x => x.Clicks)
                .Take(10)
                .ToListAsync(cancellationToken);

            vm.TopApiEndpoints = await _db.ApiUsageLogs
                .AsNoTracking()
                .Where(x => x.CreatedAt >= last30 &&
                            x.CreatedAt < tomorrowUtc)
                .GroupBy(x => x.Endpoint)
                .Select(g =>
                    new AdminDashboardViewModel.TopApiEndpointItem
                    {
                        Endpoint = g.Key,
                        Requests = g.Count(),
                        Errors = g.Count(x => x.StatusCode >= 400),
                        LastRequestedAt = g.Max(x => x.CreatedAt)
                    })
                .OrderByDescending(x => x.Requests)
                .Take(10)
                .ToListAsync(cancellationToken);

            vm.RecentClicks = await _db.ClickLogs
                .AsNoTracking()
                .OrderByDescending(x => x.ClickedAt)
                .Take(10)
                .Select(x =>
                    new AdminDashboardViewModel.RecentClickItem
                    {
                        ClickedAt = x.ClickedAt,
                        ClickType = x.ClickType,
                        ProductTitle =
                            x.Product == null
                                ? null
                                : x.Product.Title,
                        UtmSource = x.UtmSource,
                        IsSocialTraffic = x.IsSocialTraffic
                    })
                .ToListAsync(cancellationToken);

            return View(vm);
        }
    }
}
