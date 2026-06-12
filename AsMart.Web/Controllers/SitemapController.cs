// Controllers/SitemapController.cs
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsMart.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Controllers
{
    public class SitemapController : Controller
    {
        private string BaseUrl() => $"{Request.Scheme}://{Request.Host}";

        // =========================================================
        // SITEMAP INDEX
        // =========================================================
        [HttpGet("/sitemap.xml")]
        public IActionResult Index()
        {
            var baseUrl = BaseUrl();

            var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                        <sitemapindex xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">
                          <sitemap><loc>{baseUrl}/sitemap-pages.xml</loc></sitemap>
                          <sitemap><loc>{baseUrl}/sitemap-products.xml</loc></sitemap>
                          <sitemap><loc>{baseUrl}/sitemap-blogs.xml</loc></sitemap>
                          <sitemap><loc>{baseUrl}/sitemap-categories.xml</loc></sitemap>
                           <sitemap><loc>{baseUrl}/sitemap-guides.xml</loc></sitemap>
                        </sitemapindex>";

            return Content(xml, "application/xml", Encoding.UTF8);
        }

        // =========================================================
        // STATIC PAGES (ONLY KEEP REAL PUBLIC ROUTES)
        // - Removed /contact, /terms (often not present)
        // - Add /collections or /categories only if those routes exist
        // =========================================================
        [HttpGet("/sitemap-pages.xml")]
        public IActionResult Pages()
        {
            var baseUrl = BaseUrl();

            var urls = new[]
            {
                "/",            // Home
                "/catalog",     // Shop/Catalog
                "/blog",        // Blog listing
                "/privacy",     // Privacy
                "/about"        // About
                // "/collections", // Uncomment ONLY if you have a public route
                // "/categories"  // Uncomment ONLY if you have a public route
            };

            var sb = new StringBuilder();
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            sb.AppendLine(@"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

            foreach (var u in urls.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{baseUrl}{u}</loc>");
                sb.AppendLine("    <changefreq>weekly</changefreq>");
                sb.AppendLine("    <priority>0.8</priority>");
                sb.AppendLine("  </url>");
            }

            sb.AppendLine("</urlset>");
            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }

        // =========================================================
        // PRODUCTS
        // FIXES:
        // - Exclude inactive products
        // - URL-encode slug safely
        // - Add <lastmod> from Product.UpdatedAt (available in your Product entity)
        // =========================================================
        [HttpGet("/sitemap-products.xml")]
        public async Task<IActionResult> Products([FromServices] ApplicationDbContext db)
        {
            var baseUrl = BaseUrl();

            var items = await db.Products
                .AsNoTracking()
                .Where(p => p.IsActive && p.Slug != null && p.Slug.Trim() != "")
                .Select(p => new
                {
                    Slug = p.Slug!,
                    LastMod = p.UpdatedAt
                })
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            sb.AppendLine(@"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

            foreach (var it in items
                .GroupBy(x => (x.Slug ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(x => x.LastMod).First())
                .OrderBy(x => x.Slug, StringComparer.OrdinalIgnoreCase))
            {
                var safeSlug = Uri.EscapeDataString(it.Slug.Trim());

                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{baseUrl}/product/{safeSlug}</loc>");
                sb.AppendLine($"    <lastmod>{it.LastMod.ToUniversalTime():yyyy-MM-dd}</lastmod>");
                sb.AppendLine("    <changefreq>weekly</changefreq>");
                sb.AppendLine("    <priority>0.9</priority>");
                sb.AppendLine("  </url>");
            }

            sb.AppendLine("</urlset>");
            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }

        // =========================================================
        // BLOGS
        // FIXES:
        // - Keep only published blogs
        // - URL-encode slug safely
        // NOTE: lastmod is not added because your BlogPost fields
        // (UpdatedAt/PublishedAt) were not provided here.
        // Add it later if you have UpdatedAt/PublishedAt.
        // =========================================================
        [HttpGet("/sitemap-blogs.xml")]
        public async Task<IActionResult> Blogs([FromServices] ApplicationDbContext db)
        {
            var baseUrl = BaseUrl();

            var slugs = await db.BlogPosts
                .AsNoTracking()
                .Where(b => b.IsPublished && b.Slug != null && b.Slug.Trim() != "")
                .Select(b => b.Slug!)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            sb.AppendLine(@"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

            foreach (var rawSlug in slugs
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var safeSlug = Uri.EscapeDataString(rawSlug);

                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{baseUrl}/blog/{safeSlug}</loc>");
                sb.AppendLine("    <changefreq>monthly</changefreq>");
                sb.AppendLine("    <priority>0.7</priority>");
                sb.AppendLine("  </url>");
            }

            sb.AppendLine("</urlset>");
            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }

        // =========================================================
        // CATEGORIES
        // FIXES:
        // - Exclude inactive categories
        // - URL-encode slug safely
        // =========================================================
        [HttpGet("/sitemap-categories.xml")]
        public async Task<IActionResult> Categories([FromServices] ApplicationDbContext db)
        {
            var baseUrl = BaseUrl();

            var slugs = await db.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && c.Slug != null && c.Slug.Trim() != "")
                .Select(c => c.Slug!)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            sb.AppendLine(@"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

            foreach (var rawSlug in slugs
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var safeSlug = Uri.EscapeDataString(rawSlug);

                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{baseUrl}/category/{safeSlug}</loc>");
                sb.AppendLine("    <changefreq>weekly</changefreq>");
                sb.AppendLine("    <priority>0.6</priority>");
                sb.AppendLine("  </url>");
            }

            sb.AppendLine("</urlset>");
            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }

        

        [HttpGet("/sitemap-guides.xml")]
        public async Task<IActionResult> Guides([FromServices] ApplicationDbContext db)
        {
            var baseUrl = BaseUrl();

            var items = await db.SeoPages
                .AsNoTracking()
                .Where(x => x.Status == 1 && x.Slug != null && x.Slug.Trim() != "")
                .Select(x => new
                {
                    Slug = x.Slug!,
                    LastMod = x.UpdatedAt
                })
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            sb.AppendLine(@"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

            foreach (var it in items
                .GroupBy(x => (x.Slug ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(x => x.LastMod).First())
                .OrderBy(x => x.Slug, StringComparer.OrdinalIgnoreCase))
            {
                var safeSlug = Uri.EscapeDataString(it.Slug.Trim());

                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{baseUrl}/guides/{safeSlug}</loc>");
                sb.AppendLine($"    <lastmod>{it.LastMod.ToUniversalTime():yyyy-MM-dd}</lastmod>");
                sb.AppendLine("    <changefreq>weekly</changefreq>");
                sb.AppendLine("    <priority>0.7</priority>");
                sb.AppendLine("  </url>");
            }

            sb.AppendLine("</urlset>");
            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }

    }
}
