namespace AsMart.Web.Models.DTOs
{
    public class PublicSeoPageApiDto
    {
        public int Id { get; set; }
        public string Slug { get; set; } = "";
        public string Title { get; set; } = "";
        public string? MetaDescription { get; set; }
        public string? H1 { get; set; }
        public string TemplateKey { get; set; } = "";
        public string TargetKeyword { get; set; } = "";
        public string? Brand { get; set; }
        public decimal? PriceMin { get; set; }
        public decimal? PriceMax { get; set; }
        public string SortMode { get; set; } = "";
        public DateTime? PublishedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategorySlug { get; set; }

        public string Url { get; set; } = "";
    }

    public class PublicSeoPageDetailApiDto : PublicSeoPageApiDto
    {
        public string? IntroHtml { get; set; }
        public string? BodyHtml { get; set; }
        public string? FaqJson { get; set; }
    }

    public class PublicSeoPageListApiDto
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<PublicSeoPageApiDto> Items { get; set; } = new();
    }

    public class PublicSeoPageCategoryApiDto
    {
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public string CategorySlug { get; set; } = "";
        public int PageCount { get; set; }
    }

    public class PublicSeoPageStatsApiDto
    {
        public int TotalPages { get; set; }
        public int PublishedPages { get; set; }
        public int Categories { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    public class PublicSeoPageHomeWidgetApiDto
    {
        public List<PublicSeoPageApiDto> Latest { get; set; } = new();
        public List<PublicSeoPageApiDto> Random { get; set; } = new();
        public List<PublicSeoPageCategoryApiDto> Categories { get; set; } = new();
    }

    public class PublicSeoPageSitemapApiDto
    {
        public string Slug { get; set; } = "";
        public string Url { get; set; } = "";
        public DateTime UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
    }

    public class PublicSeoPageAiApiDto
    {
        public string Title { get; set; } = "";
        public string? Summary { get; set; }
        public string Url { get; set; } = "";
        public string? Category { get; set; }
        public string TargetKeyword { get; set; } = "";
    }
}