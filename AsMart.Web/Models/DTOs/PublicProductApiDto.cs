namespace AsMart.Web.Models.DTOs
{
    public class PublicProductApiDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? ShortDescription { get; set; }
        public string? Brand { get; set; }
        public decimal? Price { get; set; }
        public decimal? ListPrice { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal? Rating { get; set; }
        public int? RatingCount { get; set; }
        public string? MainImageUrl { get; set; }
        public string ProductUrl { get; set; } = "";
        public string BuyUrl { get; set; } = "";
        public bool IsFeatured { get; set; }
        public bool IsDealOfTheDay { get; set; }
        public int ClickCount { get; set; }
        public List<string> Categories { get; set; } = new();
    }

    public class ProductHomeWidgetApiDto
    {
        public List<PublicProductApiDto> Featured { get; set; } = new();
        public List<PublicProductApiDto> Deals { get; set; } = new();
        public List<PublicProductApiDto> Popular { get; set; } = new();
        public List<PublicProductApiDto> Latest { get; set; } = new();
    }

    public class PublicCategoryApiDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public int ProductCount { get; set; }
    }

    public class PublicCollectionApiDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public int ProductCount { get; set; }
    }

    public class PublicBlogApiDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? MetaDescription { get; set; }
        public string? OgImageUrl { get; set; }
        public string BlogUrl { get; set; } = "";
    }
}