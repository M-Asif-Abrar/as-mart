using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Controllers;

public sealed class HomeController : Controller
{
    private const int DefaultProductCount = 24;

    private readonly ApplicationDbContext _db;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ApplicationDbContext db,
        ILogger<HomeController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Landing page containing featured products only.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        ViewData["Title"] =
            "Featured Amazon Products and Smart Picks | As-Mart";

        ViewData["MetaDescription"] =
            "Browse featured Amazon products selected by As-Mart, including electronics, gadgets, home products and everyday essentials.";

        var products = await GetFeaturedProductsAsync(cancellationToken);

        return View(products);
    }

    /// <summary>
    /// Displays active Deals of the Day.
    /// URL: /deals
    /// </summary>
    [HttpGet("/deals")]
    public async Task<IActionResult> Deals(
        CancellationToken cancellationToken)
    {
        ViewData["Title"] =
            "Deals of the Day and Amazon Discounts | As-Mart";

        ViewData["MetaDescription"] =
            "Discover active Deals of the Day, discounted Amazon products and carefully selected offers on As-Mart.";

        var products = await _db.Products
            .AsNoTracking()
            .Where(product =>
                product.IsActive &&
                product.IsDealOfTheDay)
            .OrderByDescending(product => product.UpdatedAt)
            .ThenByDescending(product => product.CreatedAt)
            .Take(DefaultProductCount)
            .ToListAsync(cancellationToken);

        return View(products);
    }

    /// <summary>
    /// Displays products ordered by recorded product-page visits.
    /// URL: /top-visited
    /// </summary>
    [HttpGet("/top-visited")]
    public async Task<IActionResult> TopVisited(
        CancellationToken cancellationToken)
    {
        ViewData["Title"] =
            "Most Visited Products | As-Mart";

        ViewData["MetaDescription"] =
            "Browse the most visited and popular products on As-Mart, ranked by customer interest and product-page visits.";

        var products = await _db.Products
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderByDescending(product => product.ClickCount)
            .ThenByDescending(product => product.UpdatedAt)
            .Take(DefaultProductCount)
            .ToListAsync(cancellationToken);

        return View(products);
    }

    private async Task<List<Product>> GetFeaturedProductsAsync(
        CancellationToken cancellationToken)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(product =>
                product.IsActive &&
                product.IsFeatured)
            .OrderByDescending(product => product.UpdatedAt)
            .ThenByDescending(product => product.CreatedAt)
            .Take(DefaultProductCount)
            .ToListAsync(cancellationToken);
    }

    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult About()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Artical()
    {
        return View();
    }

    [HttpGet("/api-documentation")]
    public IActionResult ApiDocumentation()
    {
        ViewData["Title"] = "As-Mart Public Product API Documentation";
        return View();
    }
}