namespace AsMart.Web.Models.Entities
{
    public class SeoPageProductSnapshot
    {
        public int Id { get; set; }
        public int SeoPageId { get; set; }
        public int ProductId { get; set; }
        public int RankNo { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
