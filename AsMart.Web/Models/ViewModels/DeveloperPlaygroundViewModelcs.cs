using Microsoft.AspNetCore.Mvc.Rendering;

namespace AsMart.Web.Models.ViewModels
{
    public sealed class DeveloperPlaygroundViewModel
    {
        public int? ApiClientId { get; set; }

        public string EndpointKey { get; set; } =
            "products-featured";

        public string? Query { get; set; }

        public int Count { get; set; } = 6;

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public decimal? MinimumPrice { get; set; }

        public decimal? MaximumPrice { get; set; }

        public List<SelectListItem> ApiClients { get; set; } = [];

        public List<DeveloperPlaygroundEndpointVm> Endpoints { get; set; } = [];

        public DeveloperPlaygroundResultVm? Result { get; set; }
    }

    public sealed class DeveloperPlaygroundEndpointVm
    {
        public string Key { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string Method { get; init; } = "GET";

        public string Path { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public bool SupportsCount { get; init; }

        public bool SupportsQuery { get; init; }

        public bool SupportsPaging { get; init; }

        public bool SupportsPriceRange { get; init; }
    }

    public sealed class DeveloperPlaygroundResultVm
    {
        public string RequestUrl { get; init; } = string.Empty;

        public int StatusCode { get; init; }

        public bool IsSuccess { get; init; }

        public long ResponseTimeMs { get; init; }

        public string ResponseBody { get; init; } = string.Empty;

        public string CurlExample { get; init; } = string.Empty;

        public string CSharpExample { get; init; } = string.Empty;

        public IReadOnlyDictionary<string, string> ResponseHeaders { get; init; }
            = new Dictionary<string, string>();
    }
}