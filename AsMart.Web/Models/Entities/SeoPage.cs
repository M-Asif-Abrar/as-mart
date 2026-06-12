// Models/Entities/SeoPage.cs
using System;

namespace AsMart.Web.Models.Entities
{
    public class SeoPage
    {
        public int Id { get; set; }

        public string Slug { get; set; } = "";
        public string Title { get; set; } = "";

        public string? MetaDescription { get; set; }
        public string? H1 { get; set; }

        public string TemplateKey { get; set; } = "";
        public string TargetKeyword { get; set; } = "";

        public int? CategoryId { get; set; }
        public string? Brand { get; set; }

        public decimal? PriceMin { get; set; }
        public decimal? PriceMax { get; set; }

        public string? RulesJson { get; set; }
        public string? IntroHtml { get; set; }
        public string? BodyHtml { get; set; }
        public string? FaqJson { get; set; }

        public string SortMode { get; set; } = "rank";
        public byte Status { get; set; } = 0;

        public DateTime? PublishedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
