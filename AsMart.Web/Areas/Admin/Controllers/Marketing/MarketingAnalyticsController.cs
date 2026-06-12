using AsMart.Web.Data;
using AsMart.Web.Models.Entities.Marketing;
using AsMart.Web.Models.ViewModels.Marketing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Areas.Admin.Controllers.Marketing
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MarketingAnalyticsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MarketingAnalyticsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var utcToday = DateTime.UtcNow.Date;
            var last7 = utcToday.AddDays(-6);
            var last30 = utcToday.AddDays(-29);

            var totalQueueItems = await _db.MarketingPostingQueue.CountAsync();
            var postedItems = await _db.MarketingPostingQueue.CountAsync(x => x.Status == MarketingQueueStatus.Posted);
            var failedItems = await _db.MarketingPostingQueue.CountAsync(x => x.Status == MarketingQueueStatus.Failed);

            var completedItems = postedItems + failedItems;

            var vm = new MarketingAnalyticsViewModel
            {
                TotalCampaigns = await _db.MarketingCampaigns.CountAsync(),
                TotalQueueItems = totalQueueItems,
                PostedItems = postedItems,
                FailedItems = failedItems,
                ScheduledItems = await _db.MarketingPostingQueue.CountAsync(x => x.Status == MarketingQueueStatus.Scheduled),
                PendingItems = await _db.MarketingPostingQueue.CountAsync(x => x.Status == MarketingQueueStatus.Pending),

                PostsToday = await _db.MarketingPostingQueue.CountAsync(x =>
                    x.PostedAt >= utcToday && x.PostedAt < utcToday.AddDays(1)),

                PostsLast7Days = await _db.MarketingPostingQueue.CountAsync(x =>
                    x.PostedAt >= last7 && x.PostedAt < utcToday.AddDays(1)),

                PostsLast30Days = await _db.MarketingPostingQueue.CountAsync(x =>
                    x.PostedAt >= last30 && x.PostedAt < utcToday.AddDays(1)),

                SuccessRate = completedItems > 0
                    ? Math.Round((decimal)postedItems / completedItems * 100, 2)
                    : 0,

                FailureRate = completedItems > 0
                    ? Math.Round((decimal)failedItems / completedItems * 100, 2)
                    : 0
            };

            vm.CampaignPerformance = await _db.MarketingCampaigns
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new MarketingAnalyticsViewModel.CampaignPerformanceItem
                {
                    CampaignId = x.Id,
                    CampaignTitle = x.Title,
                    CampaignStatus = x.Status,
                    TotalQueueItems = x.PostingQueueItems.Count(),
                    PostedItems = x.PostingQueueItems.Count(q => q.Status == MarketingQueueStatus.Posted),
                    FailedItems = x.PostingQueueItems.Count(q => q.Status == MarketingQueueStatus.Failed),
                    ScheduledItems = x.PostingQueueItems.Count(q => q.Status == MarketingQueueStatus.Scheduled)
                })
                .Take(20)
                .ToListAsync();

            foreach (var item in vm.CampaignPerformance)
            {
                var completed = item.PostedItems + item.FailedItems;
                item.SuccessRate = completed > 0
                    ? Math.Round((decimal)item.PostedItems / completed * 100, 2)
                    : 0;
            }

            vm.TargetPerformance = await _db.SocialTargets
                .AsNoTracking()
                .Where(x => x.TargetType == MarketingTargetType.FacebookGroup ||
                            x.TargetType == MarketingTargetType.FacebookPage)
                .OrderByDescending(x => x.LastPostedAt)
                .Select(x => new MarketingAnalyticsViewModel.TargetPerformanceItem
                {
                    TargetId = x.Id,
                    TargetName = x.Name,
                    TargetType = x.TargetType,
                    LastPostedAt = x.LastPostedAt,
                    TotalQueueItems = _db.MarketingPostingQueue.Count(q => q.SocialTargetId == x.Id),
                    PostedItems = _db.MarketingPostingQueue.Count(q => q.SocialTargetId == x.Id && q.Status == MarketingQueueStatus.Posted),
                    FailedItems = _db.MarketingPostingQueue.Count(q => q.SocialTargetId == x.Id && q.Status == MarketingQueueStatus.Failed)
                })
                .Take(20)
                .ToListAsync();

            foreach (var item in vm.TargetPerformance)
            {
                var completed = item.PostedItems + item.FailedItems;
                item.SuccessRate = completed > 0
                    ? Math.Round((decimal)item.PostedItems / completed * 100, 2)
                    : 0;
            }

            var dailyRaw = await _db.MarketingPostingQueue
                .AsNoTracking()
                .Where(x => x.CreatedAt >= last30)
                .GroupBy(x => x.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Posted = g.Count(x => x.Status == MarketingQueueStatus.Posted),
                    Failed = g.Count(x => x.Status == MarketingQueueStatus.Failed),
                    Scheduled = g.Count(x => x.Status == MarketingQueueStatus.Scheduled)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            vm.DailyPostingStats = dailyRaw
                .Select(x => new MarketingAnalyticsViewModel.DailyPostingItem
                {
                    Date = x.Date,
                    PostedItems = x.Posted,
                    FailedItems = x.Failed,
                    ScheduledItems = x.Scheduled
                })
                .ToList();

            return View(vm);
        }
    }
}