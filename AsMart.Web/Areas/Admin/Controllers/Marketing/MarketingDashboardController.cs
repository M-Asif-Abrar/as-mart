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
    public class MarketingDashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MarketingDashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var utcToday = DateTime.UtcNow.Date;
            var last7 = utcToday.AddDays(-6);
            var last30 = utcToday.AddDays(-29);

            var vm = new MarketingDashboardViewModel
            {
                TotalChannels = await _db.MarketingChannels.CountAsync(),
                TotalSocialAccounts = await _db.SocialAccounts.CountAsync(),
                TotalFacebookTargets = await _db.SocialTargets.CountAsync(x =>
                    x.TargetType == MarketingTargetType.FacebookGroup ||
                    x.TargetType == MarketingTargetType.FacebookPage),
                ActiveFacebookTargets = await _db.SocialTargets.CountAsync(x =>
                    x.IsActive &&
                    (x.TargetType == MarketingTargetType.FacebookGroup ||
                     x.TargetType == MarketingTargetType.FacebookPage)),
                TotalCampaigns = await _db.MarketingCampaigns.CountAsync(),
                DraftCampaigns = await _db.MarketingCampaigns.CountAsync(x => x.Status == MarketingCampaignStatus.Draft),
                ReadyCampaigns = await _db.MarketingCampaigns.CountAsync(x => x.Status == MarketingCampaignStatus.Ready),
                ScheduledCampaigns = await _db.MarketingCampaigns.CountAsync(x => x.Status == MarketingCampaignStatus.Scheduled),
                RunningCampaigns = await _db.MarketingCampaigns.CountAsync(x => x.Status == MarketingCampaignStatus.Running),
                CompletedCampaigns = await _db.MarketingCampaigns.CountAsync(x => x.Status == MarketingCampaignStatus.Completed),
                PendingQueueItems = await _db.MarketingPostingQueue.CountAsync(x => x.Status == MarketingQueueStatus.Pending),
                ScheduledQueueItems = await _db.MarketingPostingQueue.CountAsync(x => x.Status == MarketingQueueStatus.Scheduled),
                PostedQueueItems = await _db.MarketingPostingQueue.CountAsync(x => x.Status == MarketingQueueStatus.Posted),
                FailedQueueItems = await _db.MarketingPostingQueue.CountAsync(x => x.Status == MarketingQueueStatus.Failed),
                PostsToday = await _db.MarketingPostingQueue.CountAsync(x =>
                    x.PostedAt >= utcToday && x.PostedAt < utcToday.AddDays(1)),
                PostsLast7Days = await _db.MarketingPostingQueue.CountAsync(x =>
                    x.PostedAt >= last7 && x.PostedAt < utcToday.AddDays(1)),
                PostsLast30Days = await _db.MarketingPostingQueue.CountAsync(x =>
                    x.PostedAt >= last30 && x.PostedAt < utcToday.AddDays(1))
            };

            vm.RecentCampaigns = await _db.MarketingCampaigns
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Take(10)
                .Select(x => new MarketingDashboardViewModel.RecentCampaignItem
                {
                    Id = x.Id,
                    Title = x.Title,
                    SourceType = x.SourceType,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt,
                    TotalQueueItems = x.PostingQueueItems.Count,
                    PostedItems = x.PostingQueueItems.Count(q => q.Status == MarketingQueueStatus.Posted),
                    FailedItems = x.PostingQueueItems.Count(q => q.Status == MarketingQueueStatus.Failed)
                })
                .ToListAsync();

            vm.RecentQueueItems = await _db.MarketingPostingQueue
                .AsNoTracking()
                .Include(x => x.MarketingCampaign)
                .Include(x => x.SocialTarget)
                .OrderByDescending(x => x.CreatedAt)
                .Take(15)
                .Select(x => new MarketingDashboardViewModel.RecentQueueItem
                {
                    Id = x.Id,
                    CampaignTitle = x.MarketingCampaign != null ? x.MarketingCampaign.Title : "",
                    TargetName = x.SocialTarget != null ? x.SocialTarget.Name : "",
                    TargetType = x.SocialTarget != null ? x.SocialTarget.TargetType : MarketingTargetType.FacebookGroup,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt,
                    ScheduledAt = x.ScheduledAt,
                    PostedAt = x.PostedAt
                })
                .ToListAsync();

            vm.TopTargets = await _db.MarketingPostingQueue
                .AsNoTracking()
                .Include(x => x.SocialTarget)
                .Where(x => x.SocialTarget != null)
                .GroupBy(x => new
                {
                    x.SocialTargetId,
                    TargetName = x.SocialTarget!.Name,
                    x.SocialTarget.TargetType
                })
                .Select(g => new MarketingDashboardViewModel.TopTargetItem
                {
                    TargetId = g.Key.SocialTargetId,
                    TargetName = g.Key.TargetName,
                    TargetType = g.Key.TargetType,
                    TotalPosts = g.Count(),
                    PostedPosts = g.Count(x => x.Status == MarketingQueueStatus.Posted),
                    FailedPosts = g.Count(x => x.Status == MarketingQueueStatus.Failed)
                })
                .OrderByDescending(x => x.TotalPosts)
                .Take(10)
                .ToListAsync();

            return View(vm);
        }
    }
}