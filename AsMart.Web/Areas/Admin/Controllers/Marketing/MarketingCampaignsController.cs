using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using AsMart.Web.Models.Entities.Marketing;
using AsMart.Web.Models.ViewModels.Marketing;
using AsMart.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Areas.Admin.Controllers.Marketing
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MarketingCampaignsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ISlugService _slugService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MarketingCampaignsController(
            ApplicationDbContext db,
            ISlugService slugService,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _slugService = slugService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var campaigns = await _db.MarketingCampaigns
                .AsNoTracking()
                .Include(x => x.Product)
                .Include(x => x.BlogPost)
                .Include(x => x.PostingQueueItems)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(campaigns);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new MarketingCampaignCreateViewModel
            {
                ScheduledStartAt = DateTime.Now.AddMinutes(15),
                MinDelayMinutes = 20,
                MaxDelayMinutes = 60,
                UTMSource = "facebook",
                UTMMedium = "group"
            };

            await PopulateCreateOptionsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MarketingCampaignCreateViewModel vm)
        {
            var scheduledStartLocal = vm.ScheduledStartAt ?? DateTime.Now.AddMinutes(15);

            if (vm.MaxDelayMinutes < vm.MinDelayMinutes)
            {
                ModelState.AddModelError(nameof(vm.MaxDelayMinutes), "Maximum delay must be greater than or equal to minimum delay.");
            }

            if (vm.SourceType == MarketingCampaignSourceType.Product && vm.ProductId == null)
            {
                ModelState.AddModelError(nameof(vm.ProductId), "Please select a product.");
            }

            if (vm.SourceType == MarketingCampaignSourceType.BlogPost && vm.BlogPostId == null)
            {
                ModelState.AddModelError(nameof(vm.BlogPostId), "Please select a blog post.");
            }

            if (vm.SelectedTargetIds == null || !vm.SelectedTargetIds.Any())
            {
                ModelState.AddModelError(nameof(vm.SelectedTargetIds), "Please select at least one Facebook group or page.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateCreateOptionsAsync(vm);
                return View(vm);
            }

            await HydrateSourceDataAsync(vm);

            var slugInput = string.IsNullOrWhiteSpace(vm.Slug) ? vm.Title : vm.Slug;
            var slug = await GenerateUniqueCampaignSlugAsync(slugInput);

            var campaign = new MarketingCampaign
            {
                Title = vm.Title.Trim(),
                Slug = slug,
                SourceType = vm.SourceType,
                ProductId = vm.SourceType == MarketingCampaignSourceType.Product ? vm.ProductId : null,
                BlogPostId = vm.SourceType == MarketingCampaignSourceType.BlogPost ? vm.BlogPostId : null,
                CampaignUrl = string.IsNullOrWhiteSpace(vm.CampaignUrl) ? null : vm.CampaignUrl.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(vm.ImageUrl) ? null : vm.ImageUrl.Trim(),
                ShortDescription = string.IsNullOrWhiteSpace(vm.ShortDescription) ? null : vm.ShortDescription.Trim(),
                Status = MarketingCampaignStatus.Scheduled,
                ScheduledStartAt = scheduledStartLocal,
                MinDelayMinutes = vm.MinDelayMinutes,
                MaxDelayMinutes = vm.MaxDelayMinutes,
                UTMSource = string.IsNullOrWhiteSpace(vm.UTMSource) ? "facebook" : vm.UTMSource.Trim(),
                UTMMedium = string.IsNullOrWhiteSpace(vm.UTMMedium) ? "group" : vm.UTMMedium.Trim(),
                UTMCampaign = string.IsNullOrWhiteSpace(vm.UTMCampaign) ? slug : vm.UTMCampaign.Trim(),
                CreatedByUserId = _userManager.GetUserId(User),
                CreatedAt = DateTime.UtcNow
            };

            _db.MarketingCampaigns.Add(campaign);
            await _db.SaveChangesAsync();

            var captions = BuildCaptions(vm);

            for (var i = 0; i < captions.Count; i++)
            {
                _db.MarketingCaptionVariations.Add(new MarketingCaptionVariation
                {
                    MarketingCampaignId = campaign.Id,
                    CaptionText = captions[i],
                    SortOrder = i + 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();

            var captionVariations = await _db.MarketingCaptionVariations
                .Where(x => x.MarketingCampaignId == campaign.Id && x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();

            var targets = await _db.SocialTargets
                .Where(x => vm.SelectedTargetIds.Contains(x.Id) && x.IsActive)
                .ToListAsync();

            var startAt = scheduledStartLocal;
            var random = new Random();
            var nextSchedule = startAt;

            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var caption = captionVariations[i % captionVariations.Count];

                if (i > 0)
                {
                    var delay = random.Next(vm.MinDelayMinutes, vm.MaxDelayMinutes + 1);
                    nextSchedule = nextSchedule.AddMinutes(delay);
                }

                var finalUrl = BuildUtmUrl(
                    campaign.CampaignUrl,
                    campaign.UTMSource,
                    campaign.UTMMedium,
                    campaign.UTMCampaign,
                    $"target-{target.Id}");

                var finalPostText = BuildFinalPostText(caption.CaptionText, finalUrl);

                _db.MarketingPostingQueue.Add(new MarketingPostingQueue
                {
                    MarketingCampaignId = campaign.Id,
                    SocialTargetId = target.Id,
                    MarketingCaptionVariationId = caption.Id,
                    Status = MarketingQueueStatus.Scheduled,
                    PublishMode = target.TargetType == MarketingTargetType.FacebookPage
                        ? MarketingPublishMode.Manual
                        : MarketingPublishMode.Manual,
                    ScheduledAt = nextSchedule,
                    FinalPostText = finalPostText,
                    FinalUrlWithUtm = finalUrl,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Marketing campaign created and posting queue generated successfully.";
            return RedirectToAction(nameof(Details), new { id = campaign.Id });
        }

        public async Task<IActionResult> Details(int id)
        {
            var campaign = await _db.MarketingCampaigns
                .AsNoTracking()
                .Include(x => x.Product)
                .Include(x => x.BlogPost)
                .Include(x => x.CaptionVariations.OrderBy(c => c.SortOrder))
                .Include(x => x.PostingQueueItems.OrderBy(q => q.ScheduledAt))
                .ThenInclude(x => x.SocialTarget)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (campaign == null)
                return NotFound();

            var socialClicks = _db.ClickLogs
                .AsNoTracking()
                .Where(x =>
                    x.IsSocialTraffic &&
                    (
                        x.MarketingCampaignId == id ||
                        x.UtmCampaign == campaign.Slug ||
                        x.UtmCampaign == campaign.UTMCampaign
                    ));

                        ViewBag.TotalSocialClicks = await socialClicks.CountAsync();
                        ViewBag.FacebookClicks = await socialClicks.CountAsync(x => x.IsFacebookTraffic);
                        ViewBag.BlogLandingClicks = await socialClicks.CountAsync(x => x.ClickType == "BlogLanding");
                        ViewBag.ProductLandingClicks = await socialClicks.CountAsync(x => x.ClickType == "ProductLanding");
                        ViewBag.AmazonOutboundClicks = await socialClicks.CountAsync(x => x.ClickType == "AmazonOutbound");

                        ViewBag.TargetClicks = await socialClicks
                            .GroupBy(x => new
                            {
                                x.UtmContent,
                                TargetName = x.SocialTarget != null ? x.SocialTarget.Name : ""
                            })
                            .Select(g => new
                            {
                                TargetName = !string.IsNullOrWhiteSpace(g.Key.TargetName)
                                    ? g.Key.TargetName
                                    : (g.Key.UtmContent ?? "Unknown Target"),
                                UtmContent = g.Key.UtmContent ?? "",
                                Clicks = g.Count(),
                                FacebookClicks = g.Count(x => x.IsFacebookTraffic),
                                LastClickAt = g.Max(x => x.ClickedAt)
                            })
                            .OrderByDescending(x => x.Clicks)
                            .Take(20)
                            .ToListAsync();

                        ViewBag.DailyClicks = await socialClicks
                            .GroupBy(x => x.ClickedAt.Date)
                            .Select(g => new
                            {
                                Date = g.Key,
                                Clicks = g.Count(),
                                FacebookClicks = g.Count(x => x.IsFacebookTraffic)
                            })
                            .OrderByDescending(x => x.Date)
                            .Take(14)
                            .ToListAsync();

            return View(campaign);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var campaign = await _db.MarketingCampaigns
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (campaign == null)
                return NotFound();

            var vm = new MarketingCampaignEditViewModel
            {
                Id = campaign.Id,
                Title = campaign.Title,
                Slug = campaign.Slug,
                SourceType = campaign.SourceType,
                ProductId = campaign.ProductId,
                BlogPostId = campaign.BlogPostId,
                CampaignUrl = campaign.CampaignUrl,
                ImageUrl = campaign.ImageUrl,
                ShortDescription = campaign.ShortDescription,
                Status = campaign.Status,
                ScheduledStartAt = campaign.ScheduledStartAt,
                MinDelayMinutes = campaign.MinDelayMinutes,
                MaxDelayMinutes = campaign.MaxDelayMinutes,
                UTMSource = campaign.UTMSource,
                UTMMedium = campaign.UTMMedium,
                UTMCampaign = campaign.UTMCampaign
            };

            await PopulateEditOptionsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MarketingCampaignEditViewModel vm)
        {
            if (id != vm.Id)
                return BadRequest();

            if (vm.MaxDelayMinutes < vm.MinDelayMinutes)
                ModelState.AddModelError(nameof(vm.MaxDelayMinutes), "Maximum delay must be greater than or equal to minimum delay.");

            if (vm.SourceType == MarketingCampaignSourceType.Product && vm.ProductId == null)
                ModelState.AddModelError(nameof(vm.ProductId), "Please select a product.");

            if (vm.SourceType == MarketingCampaignSourceType.BlogPost && vm.BlogPostId == null)
                ModelState.AddModelError(nameof(vm.BlogPostId), "Please select a blog post.");

            if (!ModelState.IsValid)
            {
                await PopulateEditOptionsAsync(vm);
                return View(vm);
            }

            var campaign = await _db.MarketingCampaigns.FirstOrDefaultAsync(x => x.Id == id);

            if (campaign == null)
                return NotFound();

            var slugInput = string.IsNullOrWhiteSpace(vm.Slug) ? vm.Title : vm.Slug.Trim();
            var slug = await GenerateUniqueCampaignSlugForEditAsync(slugInput, campaign.Id);

            campaign.Title = vm.Title.Trim();
            campaign.Slug = slug;
            campaign.SourceType = vm.SourceType;
            campaign.ProductId = vm.SourceType == MarketingCampaignSourceType.Product ? vm.ProductId : null;
            campaign.BlogPostId = vm.SourceType == MarketingCampaignSourceType.BlogPost ? vm.BlogPostId : null;
            campaign.CampaignUrl = string.IsNullOrWhiteSpace(vm.CampaignUrl) ? null : vm.CampaignUrl.Trim();
            campaign.ImageUrl = string.IsNullOrWhiteSpace(vm.ImageUrl) ? null : vm.ImageUrl.Trim();
            campaign.ShortDescription = string.IsNullOrWhiteSpace(vm.ShortDescription) ? null : vm.ShortDescription.Trim();
            campaign.Status = vm.Status;
            campaign.ScheduledStartAt = vm.ScheduledStartAt;
            campaign.MinDelayMinutes = vm.MinDelayMinutes;
            campaign.MaxDelayMinutes = vm.MaxDelayMinutes;
            campaign.UTMSource = string.IsNullOrWhiteSpace(vm.UTMSource) ? "facebook" : vm.UTMSource.Trim();
            campaign.UTMMedium = string.IsNullOrWhiteSpace(vm.UTMMedium) ? "group" : vm.UTMMedium.Trim();
            campaign.UTMCampaign = string.IsNullOrWhiteSpace(vm.UTMCampaign) ? slug : vm.UTMCampaign.Trim();
            campaign.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Marketing campaign updated successfully.";
            return RedirectToAction(nameof(Details), new { id = campaign.Id });
        }

        public async Task<IActionResult> Delete(int id)
        {
            var campaign = await _db.MarketingCampaigns
                .AsNoTracking()
                .Include(x => x.Product)
                .Include(x => x.BlogPost)
                .Include(x => x.CaptionVariations)
                .Include(x => x.PostingQueueItems)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (campaign == null)
                return NotFound();

            return View(campaign);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string confirmDelete)
        {
            if (!string.Equals(confirmDelete, "DELETE", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Deletion cancelled. Type DELETE to confirm campaign deletion.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            var campaign = await _db.MarketingCampaigns
                .Include(x => x.CaptionVariations)
                .Include(x => x.PostingQueueItems)
                    .ThenInclude(x => x.Logs)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (campaign == null)
                return NotFound();

            _db.MarketingCampaigns.Remove(campaign);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Marketing campaign deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateEditOptionsAsync(MarketingCampaignEditViewModel vm)
        {
            vm.ProductOptions = await _db.Products
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Title,
                    Selected = vm.ProductId == x.Id
                })
                .ToListAsync();

            vm.BlogPostOptions = await _db.BlogPosts
                .AsNoTracking()
                .Where(x => x.IsPublished)
                .OrderByDescending(x => x.PublishedAt ?? x.CreatedAt)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Title,
                    Selected = vm.BlogPostId == x.Id
                })
                .ToListAsync();

            vm.StatusOptions = Enum.GetValues<MarketingCampaignStatus>()
                .Select(x => new SelectListItem
                {
                    Value = x.ToString(),
                    Text = x.ToString(),
                    Selected = vm.Status == x
                })
                .ToList();
        }

        private async Task<string> GenerateUniqueCampaignSlugForEditAsync(string input, int campaignId)
        {
            var baseSlug = _slugService.GenerateSlug(input);

            if (string.IsNullOrWhiteSpace(baseSlug))
                baseSlug = $"campaign-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var slug = baseSlug;
            var counter = 2;

            while (await _db.MarketingCampaigns.AnyAsync(x => x.Id != campaignId && x.Slug == slug))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkQueuePosted(int id, string? publishedPostUrl)
        {
            var item = await _db.MarketingPostingQueue.FindAsync(id);

            if (item == null)
                return NotFound();

            item.Status = MarketingQueueStatus.Posted;
            item.PostedAt = DateTime.UtcNow;
            item.PublishedPostUrl = string.IsNullOrWhiteSpace(publishedPostUrl) ? null : publishedPostUrl.Trim();
            item.LastError = null;

            _db.MarketingPostingLogs.Add(new MarketingPostingLog
            {
                MarketingPostingQueueId = item.Id,
                Status = MarketingQueueStatus.Posted,
                Message = "Marked as posted manually by admin.",
                CreatedAt = DateTime.UtcNow
            });

            var target = await _db.SocialTargets.FindAsync(item.SocialTargetId);

            if (target != null)
            {
                target.LastPostedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Queue item marked as posted.";
            return RedirectToAction(nameof(Details), new { id = item.MarketingCampaignId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkQueueFailed(int id, string? error)
        {
            var item = await _db.MarketingPostingQueue.FindAsync(id);

            if (item == null)
                return NotFound();

            item.Status = MarketingQueueStatus.Failed;
            item.LastError = string.IsNullOrWhiteSpace(error) ? "Marked as failed manually by admin." : error.Trim();
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
            return RedirectToAction(nameof(Details), new { id = item.MarketingCampaignId });
        }

        private async Task PopulateCreateOptionsAsync(MarketingCampaignCreateViewModel vm)
        {
            vm.ProductOptions = await _db.Products
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Title,
                    Selected = vm.ProductId == x.Id
                })
                .ToListAsync();

            vm.BlogPostOptions = await _db.BlogPosts
                .AsNoTracking()
                .Where(x => x.IsPublished)
                .OrderByDescending(x => x.PublishedAt ?? x.CreatedAt)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Title,
                    Selected = vm.BlogPostId == x.Id
                })
                .ToListAsync();

            vm.TargetOptions = await _db.SocialTargets
                .AsNoTracking()
                .Include(x => x.SocialAccount)
                .Where(x => x.IsActive &&
                            (x.TargetType == MarketingTargetType.FacebookGroup ||
                             x.TargetType == MarketingTargetType.FacebookPage))
                .OrderBy(x => x.TargetType)
                .ThenBy(x => x.Name)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = $"{x.Name} ({x.TargetType})",
                    Selected = vm.SelectedTargetIds.Contains(x.Id)
                })
                .ToListAsync();
        }

        private async Task HydrateSourceDataAsync(MarketingCampaignCreateViewModel vm)
        {
            if (vm.SourceType == MarketingCampaignSourceType.Product && vm.ProductId.HasValue)
            {
                var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == vm.ProductId.Value);

                if (product == null)
                    return;

                if (string.IsNullOrWhiteSpace(vm.Title))
                    vm.Title = product.Title;

                if (string.IsNullOrWhiteSpace(vm.CampaignUrl))
                    vm.CampaignUrl = Url.Action("Details", "Product", new { slug = product.Slug }, Request.Scheme);

                if (string.IsNullOrWhiteSpace(vm.ImageUrl))
                    vm.ImageUrl = product.MainImageUrl;

                if (string.IsNullOrWhiteSpace(vm.ShortDescription))
                    vm.ShortDescription = product.ShortDescription;
            }

            if (vm.SourceType == MarketingCampaignSourceType.BlogPost && vm.BlogPostId.HasValue)
            {
                var blog = await _db.BlogPosts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == vm.BlogPostId.Value);

                if (blog == null)
                    return;

                if (string.IsNullOrWhiteSpace(vm.Title))
                    vm.Title = blog.Title;

                if (string.IsNullOrWhiteSpace(vm.CampaignUrl))
                    vm.CampaignUrl = Url.Action("Details", "Blog", new { slug = blog.Slug }, Request.Scheme);

                if (string.IsNullOrWhiteSpace(vm.ImageUrl))
                    vm.ImageUrl = blog.FeaturedImageUrl ?? blog.OgImageUrl;

                if (string.IsNullOrWhiteSpace(vm.ShortDescription))
                    vm.ShortDescription = blog.MetaDescription;
            }
        }

        private List<string> BuildCaptions(MarketingCampaignCreateViewModel vm)
        {
            var captions = new List<string>();

            if (!string.IsNullOrWhiteSpace(vm.CaptionText))
                captions.Add(vm.CaptionText.Trim());

            if (!string.IsNullOrWhiteSpace(vm.CaptionText2))
                captions.Add(vm.CaptionText2.Trim());

            if (!string.IsNullOrWhiteSpace(vm.CaptionText3))
                captions.Add(vm.CaptionText3.Trim());

            return captions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private string BuildFinalPostText(string caption, string? finalUrl)
        {
            if (string.IsNullOrWhiteSpace(finalUrl))
                return caption.Trim();

            return $"{caption.Trim()}{Environment.NewLine}{Environment.NewLine}{finalUrl.Trim()}";
        }

        private string? BuildUtmUrl(string? url, string? source, string? medium, string? campaign, string? content)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var separator = url.Contains('?') ? "&" : "?";

            return $"{url.Trim()}{separator}utm_source={Uri.EscapeDataString(source ?? "facebook")}&utm_medium={Uri.EscapeDataString(medium ?? "group")}&utm_campaign={Uri.EscapeDataString(campaign ?? "campaign")}&utm_content={Uri.EscapeDataString(content ?? "target")}";
        }

        private async Task<string> GenerateUniqueCampaignSlugAsync(string input)
        {
            var baseSlug = _slugService.GenerateSlug(input);

            if (string.IsNullOrWhiteSpace(baseSlug))
                baseSlug = $"campaign-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var slug = baseSlug;
            var counter = 2;

            while (await _db.MarketingCampaigns.AnyAsync(x => x.Slug == slug))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }
    }
}