using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using AsMart.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AsMart.Web.Controllers;

public sealed class HomeController : Controller
{
    private const int PageProductLimit = 36;

    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        SetPageMetadata(
            title: "Featured Amazon Products and Smart Picks | As-Mart",
            description:
                "Explore featured products, current deals and popular Amazon products selected by As-Mart.");

        /*
         * The home page now loads only featured products.
         * Deals and top-visited products are handled by their
         * own dedicated actions and views.
         */
        var featuredProducts = await GetRandomProductsAsync(
            product =>
                product.IsActive &&
                product.IsFeatured,
            PageProductLimit,
            cancellationToken);

        var totalProducts = await _db.Products
            .AsNoTracking()
            .CountAsync(
                product => product.IsActive,
                cancellationToken);

        var featuredProductsCount = await _db.Products
            .AsNoTracking()
            .CountAsync(
                product =>
                    product.IsActive &&
                    product.IsFeatured,
                cancellationToken);

        var dealsCount = await _db.Products
            .AsNoTracking()
            .CountAsync(
                product =>
                    product.IsActive &&
                    product.IsDealOfTheDay,
                cancellationToken);

        var totalProductViews = await _db.Products
            .AsNoTracking()
            .Where(product => product.IsActive)
            .SumAsync(
                product => (long?)product.ClickCount,
                cancellationToken)
            ?? 0L;

        var averageRating = await _db.Products
            .AsNoTracking()
            .Where(product =>
                product.IsActive &&
                product.Rating.HasValue &&
                product.Rating.Value > 0)
            .AverageAsync(
                product => product.Rating,
                cancellationToken)
            ?? 0;

        var model = new HomeDashboardViewModel
        {
            FeaturedProducts = featuredProducts,
            TotalProducts = totalProducts,
            FeaturedProductsCount = featuredProductsCount,
            DealsCount = dealsCount,
            TotalProductViews = totalProductViews,
            AverageRating = averageRating
        };

        return View(model);
    }

    [HttpGet("/deals")]
    public async Task<IActionResult> Deals(
        CancellationToken cancellationToken)
    {
        SetPageMetadata(
            title: "Deals of the Day and Amazon Discounts | As-Mart",
            description:
                "Browse active Deals of the Day and selected discounted Amazon products on As-Mart.");

        /*
         * Selects 24 random active deal products on every request.
         * SQL Server translates Guid.NewGuid() into ORDER BY NEWID().
         */
        var products = await GetRandomProductsAsync(
            product =>
                product.IsActive &&
                product.IsDealOfTheDay,
            PageProductLimit,
            cancellationToken);

        return View(products);
    }

    [HttpGet("/top-visited")]
    public async Task<IActionResult> TopVisited(
        CancellationToken cancellationToken)
    {
        SetPageMetadata(
            title: "Top Visited Products | As-Mart",
            description:
                "Browse the most visited and popular products on As-Mart.");

        /*
         * First fetch the actual top 24 products by ClickCount.
         * Then randomize only their display order.
         *
         * This preserves the meaning of "Top Visited" while ensuring
         * the products appear in a different order on each page load.
         */
        var products = await _db.Products
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderByDescending(product => product.ClickCount)
            .ThenByDescending(product => product.Rating)
            .ThenByDescending(product => product.RatingCount)
            .ThenByDescending(product => product.UpdatedAt)
            .ThenByDescending(product => product.Id)
            .Take(PageProductLimit)
            .ToListAsync(cancellationToken);

        ShuffleInPlace(products);

        return View(products);
    }

    [HttpGet]
    public IActionResult Privacy()
    {
        SetPageMetadata(
            title: "Privacy Policy | As-Mart",
            description:
                "Read the As-Mart privacy policy and learn how information is handled.");

        return View();
    }

    [HttpGet]
    public IActionResult About()
    {
        SetPageMetadata(
            title: "About As-Mart",
            description:
                "Learn more about As-Mart and its curated Amazon product recommendations.");

        return View();
    }

    [HttpGet]
    public IActionResult Artical()
    {
        SetPageMetadata(
            title: "Articles | As-Mart",
            description:
                "Explore product guides, comparisons, recommendations and shopping articles from As-Mart.");

        return View();
    }

    [HttpGet("/api-documentation")]
    public IActionResult ApiDocumentation()
    {
        SetPageMetadata(
            title: "As-Mart Public Product API Documentation",
            description:
                "Read the official documentation for the As-Mart public product API.");

        return View();
    }

    /// <summary>
    /// Retrieves a random selection of products matching the supplied condition.
    /// SQL Server translates Guid.NewGuid() to NEWID().
    /// </summary>
    private async Task<List<Product>> GetRandomProductsAsync(
        Expression<Func<Product, bool>> predicate,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return [];
        }

        return await _db.Products
            .AsNoTracking()
            .Where(predicate)
            .OrderBy(product => Guid.NewGuid())
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Uses the Fisher-Yates algorithm to randomize an already-fetched list.
    /// </summary>
    private static void ShuffleInPlace<T>(IList<T> items)
    {
        for (var currentIndex = items.Count - 1;
             currentIndex > 0;
             currentIndex--)
        {
            var randomIndex = Random.Shared.Next(currentIndex + 1);

            if (randomIndex == currentIndex)
            {
                continue;
            }

            (items[currentIndex], items[randomIndex]) =
                (items[randomIndex], items[currentIndex]);
        }
    }

    private void SetPageMetadata(
        string title,
        string description)
    {
        ViewData["Title"] = title;
        ViewData["MetaDescription"] = description;
    }
}