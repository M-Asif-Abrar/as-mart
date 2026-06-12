using AsMart.Web.Data;
using AsMart.Web.Models.DTOs;
using AsMart.Web.Models.Entities;
using AsMart.Web.Models.ViewModels;
using AsMart.Web.Services;
using AsMart.Web.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace AsMart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BlogPostsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBlogRepository _blogRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISlugService _slugService;


        public BlogPostsController(
            ApplicationDbContext db,
            IBlogRepository blogRepository,
            UserManager<ApplicationUser> userManager,
            ISlugService slugService)
        {
            _db = db;
            _blogRepository = blogRepository;
            _userManager = userManager;
            _slugService = slugService;
        }


        // GET: /Admin/BlogPosts
        public async Task<IActionResult> Index()
        {
            var utcToday = DateTime.UtcNow.Date;
            var last30 = utcToday.AddDays(-29);

            // Pre-aggregate BLOG VIEWS (page opens)
            var blogViews = await _db.ClickLogs
                .Where(x => x.BlogPostId != null && x.ClickType == "BlogView")
                .GroupBy(x => x.BlogPostId!.Value)
                .Select(g => new { BlogPostId = g.Key, Total = g.Count() })
                .ToListAsync();

            var blogViews30 = await _db.ClickLogs
                .Where(x => x.BlogPostId != null && x.ClickType == "BlogView" && x.ClickedAt >= last30)
                .GroupBy(x => x.BlogPostId!.Value)
                .Select(g => new { BlogPostId = g.Key, Total = g.Count() })
                .ToListAsync();

            var totalMap = blogViews.ToDictionary(x => x.BlogPostId, x => x.Total);
            var last30Map = blogViews30.ToDictionary(x => x.BlogPostId, x => x.Total);

            var posts = await _db.BlogPosts
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Select(p => new AdminBlogPostListItemViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    IsPublished = p.IsPublished,
                    PublishedAt = p.PublishedAt,
                    AverageRating = p.AverageRating,
                    RatingCount = p.RatingCount,
                    FeaturedImageUrl = p.FeaturedImageUrl,

                    TotalClicks = 0,
                    ClicksLast30Days = 0
                })
                .ToListAsync();

            foreach (var p in posts)
            {
                p.TotalClicks = totalMap.TryGetValue(p.Id, out var t) ? t : 0;
                p.ClicksLast30Days = last30Map.TryGetValue(p.Id, out var t30) ? t30 : 0;
            }

            return View(posts);
        }



        // GET: /Admin/BlogPosts/Create
        public async Task<IActionResult> Create()
        {
            var dto = new BlogPostEditDto
            {
                IsPublished = true,
                PublishedAt = System.DateTime.UtcNow,
                // ensure collections are not null
                SelectedCategoryIds = new(),
                SelectedTagIds = new()
            };

            await PopulateCategoriesAndTagsAsync(dto);
            return View(dto);
        }

        // POST: /Admin/BlogPosts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPostEditDto dto)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAndTagsAsync(dto);
                return View(dto);
            }

            // ✅ FIX: Always generate/normalize slug
            NormalizeSlug(dto);

            var authorId = _userManager.GetUserId(User)!;
            await _blogRepository.CreateAsync(dto, authorId);

            return RedirectToAction(nameof(Index));
        }


        // GET: /Admin/BlogPosts/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _blogRepository.GetForEditAsync(id);
            if (dto == null)
                return NotFound();

            // Content in DB is HTML-encoded (&lt;h1&gt;...), decode for the editor
            dto.Content = WebUtility.HtmlDecode(dto.Content ?? string.Empty);

            // ensure collections are non-null so Contains() is safe
            dto.SelectedCategoryIds ??= new();
            dto.SelectedTagIds ??= new();

            await PopulateCategoriesAndTagsAsync(dto);
            return View(dto);
        }

        // POST: /Admin/BlogPosts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogPostEditDto dto)
        {
            if (id != dto.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAndTagsAsync(dto);
                return View(dto);
            }

            // ✅ FIX: Always generate/normalize slug
            NormalizeSlug(dto);

            var authorId = _userManager.GetUserId(User)!;
            await _blogRepository.UpdateAsync(dto, authorId);

            return RedirectToAction(nameof(Index));
        }


        // GET: /Admin/BlogPosts/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _db.BlogPosts.FindAsync(id);
            if (post == null)
                return NotFound();

            var vm = new AdminBlogPostListItemViewModel
            {
                Id = post.Id,
                Title = post.Title,
                Slug = post.Slug,
                IsPublished = post.IsPublished,
                PublishedAt = post.PublishedAt,
                AverageRating = post.AverageRating,
                RatingCount = post.RatingCount
            };

            return View(vm);
        }

        // POST: /Admin/BlogPosts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _blogRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // Populate dropdowns for categories & tags
        private async Task PopulateCategoriesAndTagsAsync(BlogPostEditDto dto)
        {
            dto.SelectedCategoryIds ??= new();
            dto.SelectedTagIds ??= new();

            var categories = await _db.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            var tags = await _db.Tags
                .OrderBy(t => t.Name)
                .ToListAsync();

            ViewBag.CategoryOptions = categories
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = dto.SelectedCategoryIds.Contains(c.Id)
                })
                .ToList();

            ViewBag.TagOptions = tags
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = t.Name,
                    Selected = dto.SelectedTagIds.Contains(t.Id)
                })
                .ToList();
        }

        private void NormalizeSlug(BlogPostEditDto dto)
        {
            var input = string.IsNullOrWhiteSpace(dto.Slug) ? dto.Title : dto.Slug;
            dto.Slug = _slugService.GenerateSlug(input);
        }

    }
}
