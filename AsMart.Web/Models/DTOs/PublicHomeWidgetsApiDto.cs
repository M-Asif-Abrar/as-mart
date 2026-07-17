namespace AsMart.Web.Models.DTOs
{
    public sealed class PublicHomeWidgetsApiDto
    {
        public ProductHomeWidgetApiDto Products { get; set; } = new();
        public List<PublicBlogApiDto> LatestBlogs { get; set; } = new();
        public List<PublicCategoryApiDto> Categories { get; set; } = new();
        public List<PublicCollectionApiDto> Collections { get; set; } = new();
        public List<PublicSeoPageApiDto> LatestSeoGuides { get; set; } = new();
        public DateTime GeneratedAtUtc { get; set; }
    }
}
