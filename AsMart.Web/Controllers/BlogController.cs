using AsMart.Web.Data;
using AsMart.Web.Models.DTOs;
using AsMart.Web.Models.Entities;
using AsMart.Web.Services.Marketing;
using AsMart.Web.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Controllers
{
    public class BlogController : Controller
    {
        private readonly IBlogRepository _blog;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;  
        private readonly IUtmTrackingService _utm;

        public BlogController(
            IBlogRepository blog,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            IUtmTrackingService utm)  
        {
            _blog = blog;
            _userManager = userManager;
            _db = db;
            _utm = utm;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var posts = await _blog.GetPublishedPostsAsync(page, 20);
            return View(posts);
        }

        [HttpGet("/blog/{slug}")]
        public async Task<IActionResult> Post(string slug)
        {
            var userId = User.Identity?.IsAuthenticated == true
                ? _userManager.GetUserId(User)
                : null;

            var post = await _blog.GetPostBySlugAsync(slug, userId);
            if (post == null) return NotFound();

            await _utm.TrackVisitAsync(HttpContext, blogPostId: post.Id, clickType: "BlogLanding");

            // ============================
            // BLOG PAGE VIEW TRACKING
            // ============================
            var ua = Request.Headers["User-Agent"].ToString();
            if (!IsBotLike(ua))
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

                // Dedup: same blog + same ip + same ua within 30 minutes => count once
                var since = DateTime.UtcNow.AddMinutes(-30);

                var alreadyViewed = await _db.ClickLogs.AnyAsync(x =>
                    x.BlogPostId == post.Id &&
                    x.ClickType == "BlogView" &&
                    x.IPAddress == ip &&
                    x.UserAgent == ua &&
                    x.ClickedAt >= since);

                if (!alreadyViewed)
                {
                    _db.ClickLogs.Add(new ClickLog
                    {
                        BlogPostId = post.Id,
                        ClickType = "BlogView",
                        ClickedAt = DateTime.UtcNow,
                        UserId = userId,
                        IPAddress = ip,
                        UserAgent = ua
                    });

                    await _db.SaveChangesAsync();
                }
            }

            // ============================
            // SEO META (OPTION A)
            // ============================
            ViewData["MetaTitle"] = !string.IsNullOrWhiteSpace(post.MetaTitle)
                ? post.MetaTitle
                : post.Title;

            ViewData["MetaDescription"] = !string.IsNullOrWhiteSpace(post.MetaDescription)
                ? post.MetaDescription
                : post.Title;

            var canonical = $"{Request.Scheme}://{Request.Host}/blog/{post.Slug}";
            ViewData["Canonical"] = canonical;

            ViewData["OgImage"] = !string.IsNullOrWhiteSpace(post.OgImageUrl)
                ? post.OgImageUrl
                : post.FeaturedImageUrl;

            return View(post);
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Rate(int postId, byte rating, string returnSlug)
        {
            var userId = _userManager.GetUserId(User)!;
            await _blog.RateAsync(postId, userId, rating);
            return RedirectToAction(nameof(Post), new { slug = returnSlug });
        }


        // GET: /Blog/MyPosts
        [Authorize]
        [HttpGet("/Blog/MyPosts")]
        public async Task<IActionResult> MyPosts()
        {
            var userId = _userManager.GetUserId(User);

            var posts = await _db.BlogPosts
                .Where(p => p.AuthorId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(posts);
        }

        [HttpGet("/blog/go/{id:int}")]
        public async Task<IActionResult> Go(int id)
        {
            var post = await _db.BlogPosts.FindAsync(id);
            if (post == null || !post.IsPublished)
                return NotFound();

            if (string.IsNullOrWhiteSpace(post.ProductPageUrl))
                return NotFound();

            // Bot/basic UA filter (reduces fake clicks)
            var ua = Request.Headers["User-Agent"].ToString();
            if (IsBotLike(ua))
                return Redirect(post.ProductPageUrl);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userId = User?.Identity?.IsAuthenticated == true ? _userManager.GetUserId(User) : null;

            // Dedup: same blog + same ip + same ua within 60 seconds => count once
            var since = DateTime.UtcNow.AddSeconds(-60);
            var already = await _db.ClickLogs.AnyAsync(x =>
                x.BlogPostId == id &&
                x.ClickType == "BlogBuy" &&
                x.IPAddress == ip &&
                x.UserAgent == ua &&
                x.ClickedAt >= since);

            if (!already)
            {
                _db.ClickLogs.Add(new ClickLog
                {
                    BlogPostId = id,
                    ClickType = "BlogBuy",
                    ClickedAt = DateTime.UtcNow,
                    UserId = userId,
                    IPAddress = ip,
                    UserAgent = ua
                });

                await _db.SaveChangesAsync();
            }

            return Redirect(post.ProductPageUrl);
        }

        // keep private inside BlogController
        private static bool IsBotLike(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent)) return true;

            var ua = userAgent.ToLowerInvariant();
            return ua.Contains("bot") ||
                   ua.Contains("crawler") ||
                   ua.Contains("spider") ||
                   ua.Contains("slurp") ||
                   ua.Contains("headless") ||
                   ua.Contains("lighthouse") ||
                   ua.Contains("curl") ||
                   ua.Contains("wget");
        }
    }
}
