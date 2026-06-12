using AsMart.Web.Data;
using AsMart.Web.Models.Entities.Marketing;
using AsMart.Web.Models.ViewModels.Marketing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Areas.Admin.Controllers.Marketing
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class FacebookTargetsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public FacebookTargetsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            await EnsureFacebookChannelAndAccountAsync();

            var targets = await _db.SocialTargets
                .AsNoTracking()
                .Include(x => x.SocialAccount)
                .ThenInclude(x => x!.MarketingChannel)
                .Where(x => x.TargetType == MarketingTargetType.FacebookGroup ||
                            x.TargetType == MarketingTargetType.FacebookPage)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(targets);
        }

        public async Task<IActionResult> Create()
        {
            await EnsureFacebookChannelAndAccountAsync();

            var vm = new FacebookTargetCreateViewModel
            {
                TargetType = MarketingTargetType.FacebookGroup,
                IsActive = true,
                DailyPostLimit = 1,
                MinDelayMinutes = 20,
                MaxDelayMinutes = 60
            };

            await PopulateSocialAccountsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FacebookTargetCreateViewModel vm)
        {
            if (vm.TargetType != MarketingTargetType.FacebookGroup &&
                vm.TargetType != MarketingTargetType.FacebookPage)
            {
                ModelState.AddModelError(nameof(vm.TargetType), "Only Facebook Group and Facebook Page targets are allowed here.");
            }

            if (vm.MaxDelayMinutes < vm.MinDelayMinutes)
            {
                ModelState.AddModelError(nameof(vm.MaxDelayMinutes), "Maximum delay must be greater than or equal to minimum delay.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateSocialAccountsAsync(vm);
                return View(vm);
            }

            var target = new SocialTarget
            {
                SocialAccountId = vm.SocialAccountId,
                TargetType = vm.TargetType,
                Name = vm.Name.Trim(),
                TargetUrl = string.IsNullOrWhiteSpace(vm.TargetUrl) ? null : vm.TargetUrl.Trim(),
                ExternalTargetId = string.IsNullOrWhiteSpace(vm.ExternalTargetId) ? null : vm.ExternalTargetId.Trim(),
                Niche = string.IsNullOrWhiteSpace(vm.Niche) ? null : vm.Niche.Trim(),
                IsActive = vm.IsActive,
                DailyPostLimit = vm.DailyPostLimit,
                MinDelayMinutes = vm.MinDelayMinutes,
                MaxDelayMinutes = vm.MaxDelayMinutes,
                Notes = string.IsNullOrWhiteSpace(vm.Notes) ? null : vm.Notes.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.SocialTargets.Add(target);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Facebook target created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var target = await _db.SocialTargets.FindAsync(id);

            if (target == null)
                return NotFound();

            var vm = new FacebookTargetCreateViewModel
            {
                Id = target.Id,
                SocialAccountId = target.SocialAccountId,
                TargetType = target.TargetType,
                Name = target.Name,
                TargetUrl = target.TargetUrl,
                ExternalTargetId = target.ExternalTargetId,
                Niche = target.Niche,
                IsActive = target.IsActive,
                DailyPostLimit = target.DailyPostLimit,
                MinDelayMinutes = target.MinDelayMinutes,
                MaxDelayMinutes = target.MaxDelayMinutes,
                Notes = target.Notes
            };

            await PopulateSocialAccountsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FacebookTargetCreateViewModel vm)
        {
            if (id != vm.Id)
                return BadRequest();

            if (vm.TargetType != MarketingTargetType.FacebookGroup &&
                vm.TargetType != MarketingTargetType.FacebookPage)
            {
                ModelState.AddModelError(nameof(vm.TargetType), "Only Facebook Group and Facebook Page targets are allowed here.");
            }

            if (vm.MaxDelayMinutes < vm.MinDelayMinutes)
            {
                ModelState.AddModelError(nameof(vm.MaxDelayMinutes), "Maximum delay must be greater than or equal to minimum delay.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateSocialAccountsAsync(vm);
                return View(vm);
            }

            var target = await _db.SocialTargets.FindAsync(id);

            if (target == null)
                return NotFound();

            target.SocialAccountId = vm.SocialAccountId;
            target.TargetType = vm.TargetType;
            target.Name = vm.Name.Trim();
            target.TargetUrl = string.IsNullOrWhiteSpace(vm.TargetUrl) ? null : vm.TargetUrl.Trim();
            target.ExternalTargetId = string.IsNullOrWhiteSpace(vm.ExternalTargetId) ? null : vm.ExternalTargetId.Trim();
            target.Niche = string.IsNullOrWhiteSpace(vm.Niche) ? null : vm.Niche.Trim();
            target.IsActive = vm.IsActive;
            target.DailyPostLimit = vm.DailyPostLimit;
            target.MinDelayMinutes = vm.MinDelayMinutes;
            target.MaxDelayMinutes = vm.MaxDelayMinutes;
            target.Notes = string.IsNullOrWhiteSpace(vm.Notes) ? null : vm.Notes.Trim();

            await _db.SaveChangesAsync();

            TempData["Success"] = "Facebook target updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var target = await _db.SocialTargets
                .AsNoTracking()
                .Include(x => x.SocialAccount)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (target == null)
                return NotFound();

            return View(target);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var target = await _db.SocialTargets.FindAsync(id);

            if (target == null)
                return NotFound();

            var hasQueue = await _db.MarketingPostingQueue.AnyAsync(x => x.SocialTargetId == id);

            if (hasQueue)
            {
                target.IsActive = false;
                await _db.SaveChangesAsync();

                TempData["Success"] = "Target has queue history, so it was deactivated instead of deleted.";
                return RedirectToAction(nameof(Index));
            }

            _db.SocialTargets.Remove(target);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Facebook target deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateSocialAccountsAsync(FacebookTargetCreateViewModel vm)
        {
            vm.SocialAccountOptions = await _db.SocialAccounts
                .AsNoTracking()
                .Include(x => x.MarketingChannel)
                .Where(x => x.IsActive && x.MarketingChannel != null && x.MarketingChannel.Platform == MarketingPlatform.Facebook)
                .OrderBy(x => x.DisplayName)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.DisplayName,
                    Selected = x.Id == vm.SocialAccountId
                })
                .ToListAsync();
        }

        private async Task EnsureFacebookChannelAndAccountAsync()
        {
            var channel = await _db.MarketingChannels
                .FirstOrDefaultAsync(x => x.Platform == MarketingPlatform.Facebook && x.Name == "Facebook");

            if (channel == null)
            {
                channel = new MarketingChannel
                {
                    Name = "Facebook",
                    Platform = MarketingPlatform.Facebook,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _db.MarketingChannels.Add(channel);
                await _db.SaveChangesAsync();
            }

            var accountExists = await _db.SocialAccounts.AnyAsync(x => x.MarketingChannelId == channel.Id);

            if (!accountExists)
            {
                _db.SocialAccounts.Add(new SocialAccount
                {
                    MarketingChannelId = channel.Id,
                    DisplayName = "Default Facebook Account",
                    PublishMode = MarketingPublishMode.Manual,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
            }
        }
    }
}