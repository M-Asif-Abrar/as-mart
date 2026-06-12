using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;

namespace AsMart.Web.Services
{
    public class SeoProductSelector
    {
        private readonly ApplicationDbContext _db;

        public SeoProductSelector(ApplicationDbContext db)
        {
            _db = db;
        }

        private sealed class Rules
        {
            public string[]? includeBrands { get; set; }
            public string[]? excludeBrands { get; set; }
            public string[]? includeAsins { get; set; }
            public string[]? excludeAsins { get; set; }
            public string[]? includeKeywords { get; set; }
            public string[]? excludeKeywords { get; set; }
        }

        private static Expression<Func<Product, bool>> BuildTitleLikeAny(IReadOnlyList<string> keywords)
        {
            var p = Expression.Parameter(typeof(Product), "p");
            var title = Expression.Property(p, nameof(Product.Title));
            var titleNotNull = Expression.NotEqual(title, Expression.Constant(null, typeof(string)));

            Expression? orBody = null;

            foreach (var kw in keywords)
            {
                var pattern = Expression.Constant($"%{kw}%", typeof(string));

                var likeCall = Expression.Call(
                    typeof(DbFunctionsExtensions),
                    nameof(DbFunctionsExtensions.Like),
                    Type.EmptyTypes,
                    Expression.Property(null, typeof(EF), nameof(EF.Functions)),
                    title,
                    pattern
                );

                orBody = orBody == null ? (Expression)likeCall : Expression.OrElse(orBody, likeCall);
            }

            var body = Expression.AndAlso(titleNotNull, orBody ?? Expression.Constant(true));
            return Expression.Lambda<Func<Product, bool>>(body, p);
        }

        public List<Product> Select(SeoPage page, int take = 12)
        {
            var q = _db.Products
                .AsNoTracking()
                .Include(p => p.ProductCategories)
                .Where(p => p.IsActive);

            if (page.CategoryId.HasValue && page.CategoryId.Value > 0)
                q = q.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == page.CategoryId.Value));

            if (!string.IsNullOrWhiteSpace(page.Brand))
                q = q.Where(p => p.Brand == page.Brand);

            if (page.PriceMin.HasValue)
                q = q.Where(p => p.Price >= page.PriceMin.Value);

            if (page.PriceMax.HasValue && page.PriceMax.Value > 0)
                q = q.Where(p => p.Price <= page.PriceMax.Value);

            Rules rules = null;
            if (!string.IsNullOrWhiteSpace(page.RulesJson))
            {
                try { rules = JsonSerializer.Deserialize<Rules>(page.RulesJson); }
                catch { rules = null; }
            }

            if (rules?.includeBrands?.Length > 0)
                q = q.Where(p => p.Brand != null && rules.includeBrands.Contains(p.Brand));

            if (rules?.excludeBrands?.Length > 0)
                q = q.Where(p => p.Brand == null || !rules.excludeBrands.Contains(p.Brand));

            if (rules?.includeAsins?.Length > 0)
                q = q.Where(p => p.ASIN != null && rules.includeAsins.Contains(p.ASIN));

            if (rules?.excludeAsins?.Length > 0)
                q = q.Where(p => p.ASIN == null || !rules.excludeAsins.Contains(p.ASIN));

            if (rules?.includeKeywords?.Length > 0)
            {
                var kws = rules.includeKeywords
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct()
                    .ToArray();

                if (kws.Length > 0)
                {
                    q = q.Where(p =>
                        p.Title != null &&
                        kws.Any(kw => EF.Functions.Like(p.Title, "%" + kw + "%"))
                    );
                }
            }

            if (rules?.excludeKeywords?.Length > 0)
            {
                foreach (var kw in rules.excludeKeywords.Where(x => !string.IsNullOrWhiteSpace(x)))
                    q = q.Where(p => p.Title == null || !EF.Functions.Like(p.Title, $"%{kw.Trim()}%"));
            }

            var sort = (page.SortMode ?? "").Trim().ToLowerInvariant();

            if (sort == "price_asc")
                return q.OrderBy(p => p.Price).Take(take).ToList();

            if (sort == "price_desc")
                return q.OrderByDescending(p => p.Price).Take(take).ToList();

            return q
                .OrderByDescending(p => p.Rating)
                .ThenByDescending(p => p.ClickCount)
                .ThenByDescending(p => p.RatingCount)
                .Take(take)
                .ToList();
        }

    }
}
