namespace AsMart.Web.Models.DTOs
{
    public class CategoryProductThumbDto
    {
        public int Id { get; set; }
        public string Slug { get; set; } = "";
        public string Title { get; set; } = "";
        public string? MainImageUrl { get; set; }
    }
}
