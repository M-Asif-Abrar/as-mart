using AsMart.Web.Data;
using AsMart.Web.Models.ViewModels.Marketing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Areas.Admin.Controllers.Marketing
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MarketingClickAnalyticsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MarketingClickAnalyticsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var socialQuery = _db.ClickLogs
                .AsNoTracking()
                .Where(x => x.IsSocialTraffic);

            var vm = new MarketingClickAnalyticsViewModel
            {
                TotalSocialClicks = await socialQuery.CountAsync(),
                FacebookClicks = await socialQuery.CountAsync(x => x.IsFacebookTraffic),
                InstagramClicks = await socialQuery.CountAsync(x => x.IsInstagramTraffic),
                PinterestClicks = await socialQuery.CountAsync(x => x.IsPinterestTraffic),
                TelegramClicks = await socialQuery.CountAsync(x => x.IsTelegramTraffic),

                BlogLandingClicks = await socialQuery.CountAsync(x => x.ClickType == "BlogLanding"),
                ProductLandingClicks = await socialQuery.CountAsync(x => x.ClickType == "ProductLanding"),
                AmazonOutboundClicks = await socialQuery.CountAsync(x => x.ClickType == "AmazonOutbound")
            };

            vm.CampaignClicks = await socialQuery
                .GroupBy(x => new
                {
                    x.MarketingCampaignId,
                    CampaignTitle = x.MarketingCampaign != null ? x.MarketingCampaign.Title : "",
                    x.UtmCampaign
                })
                .Select(g => new MarketingClickAnalyticsViewModel.CampaignClickItem
                {
                    CampaignId = g.Key.MarketingCampaignId,
                    CampaignName = !string.IsNullOrWhiteSpace(g.Key.CampaignTitle)
                        ? g.Key.CampaignTitle
                        : (g.Key.UtmCampaign ?? "Unknown Campaign"),
                    UtmCampaign = g.Key.UtmCampaign ?? "",
                    Clicks = g.Count(),
                    FacebookClicks = g.Count(x => x.IsFacebookTraffic),
                    BlogClicks = g.Count(x => x.ClickType == "BlogLanding"),
                    ProductClicks = g.Count(x => x.ClickType == "ProductLanding")
                })
                .OrderByDescending(x => x.Clicks)
                .Take(25)
                .ToListAsync();

            vm.TargetClicks = await socialQuery
                .GroupBy(x => new
                {
                    x.SocialTargetId,
                    TargetName = x.SocialTarget != null ? x.SocialTarget.Name : "",
                    x.UtmContent
                })
                .Select(g => new MarketingClickAnalyticsViewModel.TargetClickItem
                {
                    TargetId = g.Key.SocialTargetId,
                    TargetName = !string.IsNullOrWhiteSpace(g.Key.TargetName)
                        ? g.Key.TargetName
                        : (g.Key.UtmContent ?? "Unknown Target"),
                    UtmContent = g.Key.UtmContent ?? "",
                    Clicks = g.Count(),
                    FacebookClicks = g.Count(x => x.IsFacebookTraffic)
                })
                .OrderByDescending(x => x.Clicks)
                .Take(25)
                .ToListAsync();

            vm.BlogClicks = await socialQuery
                .Where(x => x.BlogPostId != null)
                .GroupBy(x => new
                {
                    BlogPostId = x.BlogPostId!.Value,
                    Title = x.BlogPost != null ? x.BlogPost.Title : "",
                    Slug = x.BlogPost != null ? x.BlogPost.Slug : ""
                })
                .Select(g => new MarketingClickAnalyticsViewModel.BlogClickItem
                {
                    BlogPostId = g.Key.BlogPostId,
                    Title = g.Key.Title,
                    Slug = g.Key.Slug,
                    Clicks = g.Count()
                })
                .OrderByDescending(x => x.Clicks)
                .Take(25)
                .ToListAsync();

            vm.ProductClicks = await socialQuery
                .Where(x => x.ProductId != null)
                .GroupBy(x => new
                {
                    ProductId = x.ProductId!.Value,
                    Title = x.Product != null ? x.Product.Title : "",
                    Slug = x.Product != null ? x.Product.Slug : ""
                })
                .Select(g => new MarketingClickAnalyticsViewModel.ProductClickItem
                {
                    ProductId = g.Key.ProductId,
                    Title = g.Key.Title,
                    Slug = g.Key.Slug,
                    Clicks = g.Count()
                })
                .OrderByDescending(x => x.Clicks)
                .Take(25)
                .ToListAsync();

            vm.UtmCampaigns = await socialQuery
                .Where(x => x.UtmCampaign != null && x.UtmCampaign != "")
                .GroupBy(x => new
                {
                    x.UtmCampaign,
                    x.UtmSource,
                    x.UtmMedium
                })
                .Select(g => new MarketingClickAnalyticsViewModel.UtmCampaignItem
                {
                    UtmCampaign = g.Key.UtmCampaign ?? "",
                    UtmSource = g.Key.UtmSource ?? "",
                    UtmMedium = g.Key.UtmMedium ?? "",
                    Clicks = g.Count(),
                    LastClickAt = g.Max(x => x.ClickedAt)
                })
                .OrderByDescending(x => x.Clicks)
                .Take(25)
                .ToListAsync();

            return View(vm);
        }
    }
}