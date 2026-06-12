// Models/Seo/SeoPagesIndexRowVm.cs
namespace AsMart.Web.Models.Seo
{
    public class SeoPagesIndexRowVm
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
        public string TargetKeyword { get; set; }
        public byte Status { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }
    }
}