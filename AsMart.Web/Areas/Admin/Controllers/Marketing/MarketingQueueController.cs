using AsMart.Web.Data;
using AsMart.Web.Models.Entities.Marketing;
using AsMart.Web.Models.ViewModels.Marketing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsMart.Web.Services.Marketing;

namespace AsMart.Web.Areas.Admin.Controllers.Marketing
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MarketingQueueController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IFacebookPagePublisher _facebookPagePublisher;

        public MarketingQueueController(
            ApplicationDbContext db,
            IFacebookPagePublisher facebookPagePublisher)
        {
            _db = db;
            _facebookPagePublisher = facebookPagePublisher;
        }

        public async Task<IActionResult> Index(string status = "all", string? search = null)
        {
            status = string.IsNullOrWhiteSpace(status) ? "all" : status.Trim().ToLowerInvariant();
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            var baseQuery = _db.MarketingPostingQueue
                .AsNoTracking()
                .Include(x => x.MarketingCampaign)
                .Include(x => x.SocialTarget)
                .AsQueryable();

            var vm = new MarketingQueueIndexViewModel
            {
                StatusFilter = status,
                Search = search,
                TotalItems = await baseQuery.CountAsync(),
                PendingItems = await baseQuery.CountAsync(x => x.Status == MarketingQueueStatus.Pending),
                ScheduledItems = await baseQuery.CountAsync(x => x.Status == MarketingQueueStatus.Scheduled),
                PostedItems = await baseQuery.CountAsync(x => x.Status == MarketingQueueStatus.Posted),
                FailedItems = await baseQuery.CountAsync(x => x.Status == MarketingQueueStatus.Failed)
            };

            var query = baseQuery;

            if (status != "all")
            {
                if (Enum.TryParse<MarketingQueueStatus>(status, true, out var parsedStatus))
                {
                    query = query.Where(x => x.Status == parsedStatus);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    (x.MarketingCampaign != null && x.MarketingCampaign.Title.Contains(search)) ||
                    (x.SocialTarget != null && x.SocialTarget.Name.Contains(search)) ||
                    (x.FinalPostText != null && x.FinalPostText.Contains(search)) ||
                    (x.FinalUrlWithUtm != null && x.FinalUrlWithUtm.Contains(search)));
            }

            vm.Items = await query
                .OrderBy(x => x.Status == MarketingQueueStatus.Posted)
                .ThenBy(x => x.ScheduledAt ?? x.CreatedAt)
                .Take(300)
                .Select(x => new MarketingQueueIndexViewModel.MarketingQueueItemViewModel
                {
                    Id = x.Id,
                    CampaignId = x.MarketingCampaignId,
                    CampaignTitle = x.MarketingCampaign != null ? x.MarketingCampaign.Title : "",
                    CampaignSlug = x.MarketingCampaign != null ? x.MarketingCampaign.Slug : "",
                    TargetName = x.SocialTarget != null ? x.SocialTarget.Name : "",
                    TargetUrl = x.SocialTarget != null ? x.SocialTarget.TargetUrl : null,
                    TargetType = x.SocialTarget != null ? x.SocialTarget.TargetType : MarketingTargetType.FacebookGroup,
                    Status = x.Status,
                    PublishMode = x.PublishMode,
                    CreatedAt = x.CreatedAt,
                    ScheduledAt = x.ScheduledAt,
                    PostedAt = x.PostedAt,
                    FinalPostText = x.FinalPostText,
                    FinalUrlWithUtm = x.FinalUrlWithUtm,
                    PublishedPostUrl = x.PublishedPostUrl,
                    LastError = x.LastError,
                    RetryCount = x.RetryCount
                })
                .ToListAsync();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPosted(int id, string? publishedPostUrl, string? returnUrl = null)
        {
            var item = await _db.MarketingPostingQueue.FindAsync(id);

            if (item == null)
                return NotFound();

            item.Status = MarketingQueueStatus.Posted;
            item.PostedAt = DateTime.UtcNow;
            item.PublishedPostUrl = string.IsNullOrWhiteSpace(publishedPostUrl) ? item.PublishedPostUrl : publishedPostUrl.Trim();
            item.LastError = null;

            _db.MarketingPostingLogs.Add(new MarketingPostingLog
            {
                MarketingPostingQueueId = item.Id,
                Status = MarketingQueueStatus.Posted,
                Message = "Marked as posted from global queue.",
                CreatedAt = DateTime.UtcNow
            });

            var target = await _db.SocialTargets.FindAsync(item.SocialTargetId);
            if (target != null)
            {
                target.LastPostedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Queue item marked as posted.";
            return RedirectSafe(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFailed(int id, string? error, string? returnUrl = null)
        {
            var item = await _db.MarketingPostingQueue.FindAsync(id);

            if (item == null)
                return NotFound();

            item.Status = MarketingQueueStatus.Failed;
            item.LastError = string.IsNullOrWhiteSpace(error) ? "Marked as failed from global queue." : error.Trim();
            item.RetryCount += 1;

            _db.MarketingPostingLogs.Add(new MarketingPostingLog
            {
                MarketingPostingQueueId = item.Id,
                Status = MarketingQueueStatus.Failed,
                Message = item.LastError,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            TempData["Success"] = "Queue item marked as failed.";
            return RedirectSafe(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetToScheduled(int id, string? returnUrl = null)
        {
            var item = await _db.MarketingPostingQueue.FindAsync(id);

            if (item == null)
                return NotFound();

            item.Status = MarketingQueueStatus.Scheduled;
            item.LastError = null;

            _db.MarketingPostingLogs.Add(new MarketingPostingLog
            {
                MarketingPostingQueueId = item.Id,
                Status = MarketingQueueStatus.Scheduled,
                Message = "Queue item reset to scheduled.",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            TempData["Success"] = "Queue item reset to scheduled.";
            return RedirectSafe(returnUrl);
        }

        private IActionResult RedirectSafe(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishFacebookPageNow(int id, string? returnUrl = null)
        {
            var result = await _facebookPagePublisher.PublishQueueItemAsync(id);

            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Facebook Page post published successfully."
                : $"Facebook publish failed: {result.ErrorMessage}";

            return RedirectSafe(returnUrl);
        }
    }
}