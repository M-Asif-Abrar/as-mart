namespace AsMart.Web.Models.Seo
{
    public class SeoPagesIndexMeta
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }

        public string Q { get; set; }
        public byte? Status { get; set; }
        public int? CategoryId { get; set; }
        public string Brand { get; set; }
        public string TemplateKey { get; set; }
        public string SortMode { get; set; }
        public decimal? PriceMin { get; set; }
        public decimal? PriceMax { get; set; }
    }
}
