using AsMart.Web.Data;
using AsMart.Web.Models.DTOs;
using AsMart.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.RegularExpressions;

namespace AsMart.Web.Services.Repositories
{
    public class BlogRepository : IBlogRepository
    {
        private readonly ApplicationDbContext _db;

        public BlogRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<BlogPostSummaryDto>> GetPublishedPostsAsync(int page, int pageSize)
        {
            var query = _db.BlogPosts
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.PublishedAt);

            var posts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new BlogPostSummaryDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    FeaturedImageUrl = p.FeaturedImageUrl,
                    PublishedAt = p.PublishedAt,
                    AverageRating = p.AverageRating,
                    RatingCount = p.RatingCount,
                    ViewCount = _db.ClickLogs.Count(x => x.BlogPostId == p.Id && x.ClickType == "BlogView"),
                    // build a clean, plain-text excerpt
                    Excerpt = StripHtmlToText(p.Content, 260),

                    CategoryNames = p.BlogPostCategories
                        .Select(x => x.Category.Name)
                        .ToList(),

                    TagNames = p.BlogPostTags
                        .Select(x => x.Tag.Name)
                        .ToList()
                })
                .ToListAsync();

            return posts;
        }


        public async Task<BlogPostDetailsDto?> GetPostBySlugAsync(string slug, string? currentUserId)
        {
            var post = await _db.BlogPosts
                .Include(p => p.Author)
                .Include(p => p.BlogPostCategories).ThenInclude(x => x.Category)
                .Include(p => p.BlogPostTags).ThenInclude(x => x.Tag)
                .Include(p => p.Ratings)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

            if (post == null) return null;

            var dto = new BlogPostDetailsDto
            {
                Id = post.Id,
                Title = post.Title,
                Slug = post.Slug,
                Content = post.Content,
                FeaturedImageUrl = post.FeaturedImageUrl,
                PublishedAt = post.PublishedAt,
                AverageRating = post.AverageRating,
                RatingCount = post.RatingCount,
                AuthorName = post.Author?.DisplayName,

                // IMPORTANT: this powers the “Buy on Amazon” button
                ProductPageUrl = post.ProductPageUrl,

                CategoryNames = post.BlogPostCategories
                    .Select(x => x.Category.Name)
                    .ToList(),
                TagNames = post.BlogPostTags
                    .Select(x => x.Tag.Name)
                    .ToList(),
                CurrentUserRating = currentUserId == null
                    ? (byte?)null
                    : post.Ratings.FirstOrDefault(r => r.UserId == currentUserId)?.Value
            };

            return dto;
        }

        public async Task<BlogPostEditDto?> GetForEditAsync(int id)
        {
            var post = await _db.BlogPosts
                .Include(p => p.BlogPostCategories)
                .Include(p => p.BlogPostTags)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return null;

            return new BlogPostEditDto
            {
                Id = post.Id,
                Title = post.Title,
                Slug = post.Slug,
                Content = post.Content,
                FeaturedImageUrl = post.FeaturedImageUrl,
                ProductPageUrl = post.ProductPageUrl,
                IsPublished = post.IsPublished,
                PublishedAt = post.PublishedAt,
                SelectedCategoryIds = post.BlogPostCategories.Select(x => x.CategoryId).ToList(),
                SelectedTagIds = post.BlogPostTags.Select(x => x.TagId).ToList()
            };
        }

        public async Task<int> CreateAsync(BlogPostEditDto dto, string authorId)
        {
            var post = new BlogPost
            {
                Title = dto.Title,
                Slug = await EnsureUniqueSlugAsync(dto.Slug),
                Content = dto.Content,
                FeaturedImageUrl = dto.FeaturedImageUrl,
                ProductPageUrl = string.IsNullOrWhiteSpace(dto.ProductPageUrl)
                    ? null
                    : dto.ProductPageUrl.Trim(),
                IsPublished = dto.IsPublished,
                PublishedAt = dto.IsPublished ? dto.PublishedAt ?? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow,
                AuthorId = authorId
            };

            _db.BlogPosts.Add(post);
            await _db.SaveChangesAsync();

            await UpdateCategoriesAndTagsAsync(post, dto);
            return post.Id;
        }

        public async Task UpdateAsync(BlogPostEditDto dto, string authorId)
        {
            var post = await _db.BlogPosts
                .Include(p => p.BlogPostCategories)
                .Include(p => p.BlogPostTags)
                .FirstOrDefaultAsync(p => p.Id == dto.Id);

            if (post == null) throw new InvalidOperationException("Post not found.");

            post.Title = dto.Title;
            post.Slug = await EnsureUniqueSlugAsync(dto.Slug, dto.Id);
            post.Content = dto.Content;
            post.FeaturedImageUrl = dto.FeaturedImageUrl;
            post.ProductPageUrl = string.IsNullOrWhiteSpace(dto.ProductPageUrl)
                ? null
                : dto.ProductPageUrl.Trim();
            post.IsPublished = dto.IsPublished;
            post.PublishedAt = dto.IsPublished
                ? dto.PublishedAt ?? post.PublishedAt ?? DateTime.UtcNow
                : null;
            post.UpdatedAt = DateTime.UtcNow;
            post.AuthorId = authorId;

            await UpdateCategoriesAndTagsAsync(post, dto);
        }

        private async Task UpdateCategoriesAndTagsAsync(BlogPost post, BlogPostEditDto dto)
        {
            _db.BlogPostCategories.RemoveRange(post.BlogPostCategories);
            _db.BlogPostTags.RemoveRange(post.BlogPostTags);

            if (dto.SelectedCategoryIds.Any())
            {
                var cats = dto.SelectedCategoryIds.Distinct()
                    .Select(id => new BlogPostCategory { BlogPostId = post.Id, CategoryId = id });
                await _db.BlogPostCategories.AddRangeAsync(cats);
            }

            if (dto.SelectedTagIds.Any())
            {
                var tags = dto.SelectedTagIds.Distinct()
                    .Select(id => new BlogPostTag { BlogPostId = post.Id, TagId = id });
                await _db.BlogPostTags.AddRangeAsync(tags);
            }

            await _db.SaveChangesAsync();
        }

        // Services/Repositories/BlogRepository.cs  (REPLACE DeleteAsync with this)
        public async Task DeleteAsync(int id)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            await _db.ClickLogs
                .Where(x => x.BlogPostId == id)
                .ExecuteDeleteAsync();

            await _db.BlogPostRatings
                .Where(x => x.BlogPostId == id)
                .ExecuteDeleteAsync();

            await _db.BlogPostCategories
                .Where(x => x.BlogPostId == id)
                .ExecuteDeleteAsync();

            await _db.BlogPostTags
                .Where(x => x.BlogPostId == id)
                .ExecuteDeleteAsync();

            var post = await _db.BlogPosts.FirstOrDefaultAsync(x => x.Id == id);
            if (post != null)
            {
                _db.BlogPosts.Remove(post);
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
        }

        public async Task RateAsync(int postId, string userId, byte rating)
        {
            rating = (byte)Math.Clamp(rating, (byte)1, (byte)5);

            var post = await _db.BlogPosts
                .Include(p => p.Ratings)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null) throw new InvalidOperationException("Post not found.");

            var existing = post.Ratings.FirstOrDefault(r => r.UserId == userId);
            if (existing == null)
            {
                var ratingEntity = new BlogPostRating
                {
                    BlogPostId = postId,
                    UserId = userId,
                    Value = rating
                };
                _db.BlogPostRatings.Add(ratingEntity);
            }
            else
            {
                existing.Value = rating;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            var stats = await _db.BlogPostRatings
                .Where(r => r.BlogPostId == postId)
                .GroupBy(r => r.BlogPostId)
                .Select(g => new { Avg = g.Average(x => x.Value), Count = g.Count() })
                .FirstAsync();

            post.AverageRating = stats.Avg;
            post.RatingCount = stats.Count;

            await _db.SaveChangesAsync();
        }

        private static string StripHtmlToText(string html, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            // 1) Remove HTML tags
            var text = Regex.Replace(html, "<.*?>", string.Empty);

            // 2) Decode HTML entities (&amp;, &nbsp;, etc.)
            text = WebUtility.HtmlDecode(text);

            // 3) Normalize whitespace
            text = Regex.Replace(text, @"\s+", " ").Trim();

            // 4) Truncate
            if (text.Length > maxLength)
                text = text.Substring(0, maxLength) + "...";

            return text;
        }

        private async Task<string> EnsureUniqueSlugAsync(string baseSlug, int? ignoreId = null)
        {
            baseSlug = (baseSlug ?? "").Trim();
            if (string.IsNullOrWhiteSpace(baseSlug))
                baseSlug = "post";

            var slug = baseSlug;
            var i = 2;

            while (await _db.BlogPosts.AnyAsync(p => p.Slug == slug && (!ignoreId.HasValue || p.Id != ignoreId.Value)))
            {
                slug = $"{baseSlug}-{i}";
                i++;
            }

            return slug;
        }

    }
}
