namespace AsMart.Web.Models.ViewModels
{
    public sealed class ErrorPageViewModel
    {
        public int StatusCode { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Hint { get; set; } = "";
        public string HomeUrl { get; set; } = "/";
        public string SearchActionUrl { get; set; } = "/search";
        public string SearchPlaceholder { get; set; } = "Search products & guides...";
        public string? RequestId { get; set; }
        public bool ShowRequestId { get; set; }
        public List<QuickLink> QuickLinks { get; set; } = new();
    }

    public sealed class QuickLink
    {
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
    }
}
