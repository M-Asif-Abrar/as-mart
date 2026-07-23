using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using AsMart.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Controllers
{
    [Authorize]
    public sealed class DeveloperPlaygroundController : Controller
    {
        private const string ApiKeyHeaderName = "X-API-Key";

        private static readonly JsonSerializerOptions PrettyJsonOptions =
            new()
            {
                WriteIndented = true
            };

        private static readonly IReadOnlyList<DeveloperPlaygroundEndpointVm>
            EndpointDefinitions =
            [
                new()
                {
                    Key = "products-featured",
                    Name = "Featured products",
                    Method = "GET",
                    Path = "api/v1/products/featured",
                    Description = "Returns featured active products.",
                    SupportsCount = true
                },
                new()
                {
                    Key = "products-latest",
                    Name = "Latest products",
                    Method = "GET",
                    Path = "api/v1/products/latest",
                    Description = "Returns latest active products.",
                    SupportsCount = true
                },
                new()
                {
                    Key = "products-popular",
                    Name = "Popular products",
                    Method = "GET",
                    Path = "api/v1/products/popular",
                    Description = "Returns popular products.",
                    SupportsCount = true
                },
                new()
                {
                    Key = "products-random",
                    Name = "Random products",
                    Method = "GET",
                    Path = "api/v1/products/random",
                    Description = "Returns random active products.",
                    SupportsCount = true
                },
                new()
                {
                    Key = "products-search",
                    Name = "Search products",
                    Method = "GET",
                    Path = "api/v1/products/search",
                    Description = "Searches products by query.",
                    SupportsCount = true,
                    SupportsQuery = true
                },
                new()
                {
                    Key = "products-price-range",
                    Name = "Products by price range",
                    Method = "GET",
                    Path = "api/v1/products/price-range",
                    Description = "Filters products by minimum and maximum price.",
                    SupportsCount = true,
                    SupportsPriceRange = true
                },
                new()
                {
                    Key = "categories",
                    Name = "Categories",
                    Method = "GET",
                    Path = "api/v1/categories",
                    Description = "Returns all public categories."
                },
                new()
                {
                    Key = "collections",
                    Name = "Collections",
                    Method = "GET",
                    Path = "api/v1/collections",
                    Description = "Returns all public collections."
                },
                new()
                {
                    Key = "blogs-latest",
                    Name = "Latest blogs",
                    Method = "GET",
                    Path = "api/v1/blogs/latest",
                    Description = "Returns latest published blogs.",
                    SupportsCount = true
                },
                new()
                {
                    Key = "seo-pages",
                    Name = "SEO pages",
                    Method = "GET",
                    Path = "api/v1/seopages",
                    Description = "Returns paginated published SEO pages.",
                    SupportsPaging = true
                },
                new()
                {
                    Key = "seo-search",
                    Name = "Search SEO pages",
                    Method = "GET",
                    Path = "api/v1/seopages/search",
                    Description = "Searches published SEO pages.",
                    SupportsQuery = true,
                    SupportsPaging = true
                },
                new()
                {
                    Key = "home-widget",
                    Name = "Home widget",
                    Method = "GET",
                    Path = "api/v1/widgets/home",
                    Description = "Returns combined home-page API resources."
                }
            ];

        private readonly ApplicationDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;

        public DeveloperPlaygroundController(
            ApplicationDbContext db,
            IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("/Developer/Playground")]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var model = new DeveloperPlaygroundViewModel();

            await PopulateOptionsAsync(
                model,
                cancellationToken);

            return View(model);
        }

        [HttpPost("/Developer/Playground")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            DeveloperPlaygroundViewModel model,
            CancellationToken cancellationToken)
        {
            await PopulateOptionsAsync(
                model,
                cancellationToken);

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var endpoint = EndpointDefinitions.FirstOrDefault(
                item => string.Equals(
                    item.Key,
                    model.EndpointKey,
                    StringComparison.OrdinalIgnoreCase));

            if (endpoint is null)
            {
                ModelState.AddModelError(
                    nameof(model.EndpointKey),
                    "Select a valid API endpoint.");

                return View(model);
            }

            ApiClient? apiClient = null;

            if (model.ApiClientId.HasValue)
            {
                apiClient = await _db.ApiClients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        client =>
                            client.Id == model.ApiClientId.Value &&
                            client.UserId == userId,
                        cancellationToken);

                if (apiClient is null)
                {
                    ModelState.AddModelError(
                        nameof(model.ApiClientId),
                        "The selected API key is unavailable.");

                    return View(model);
                }

                if (!apiClient.IsUsable)
                {
                    ModelState.AddModelError(
                        nameof(model.ApiClientId),
                        $"The selected API key is {apiClient.LifecycleStatus.ToLowerInvariant()}.");

                    return View(model);
                }
            }

            var requestUrl = BuildRequestUrl(
                endpoint,
                model);

            var client = _httpClientFactory.CreateClient(
                "DeveloperPlayground");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                requestUrl);

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            /*
             * ApiClient stores only the hash and prefix, so an existing
             * raw API key cannot be reconstructed here.
             *
             * Until encrypted key storage or a delegated playground token
             * is introduced, authenticated playground calls run anonymously.
             */
            if (apiClient is not null)
            {
                ModelState.AddModelError(
                    nameof(model.ApiClientId),
                    "Existing API keys cannot be used by the playground because AS-Mart securely stores only their hash. Select Anonymous, or create a dedicated delegated playground-token feature.");

                return View(model);
            }

            var stopwatch = Stopwatch.StartNew();

            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var responseBody = await response.Content
                .ReadAsStringAsync(cancellationToken);

            stopwatch.Stop();

            model.Result = new DeveloperPlaygroundResultVm
            {
                RequestUrl = requestUrl,
                StatusCode = (int)response.StatusCode,
                IsSuccess = response.IsSuccessStatusCode,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                ResponseBody = FormatJson(responseBody),
                ResponseHeaders = ReadResponseHeaders(response),
                CurlExample = BuildCurlExample(requestUrl),
                CSharpExample = BuildCSharpExample(requestUrl)
            };

            return View(model);
        }

        private Task PopulateOptionsAsync(
                DeveloperPlaygroundViewModel model,
                CancellationToken cancellationToken)
        {
            model.ApiClientId = null;

            model.ApiClients =
            [
                new SelectListItem
        {
            Value = string.Empty,
            Text = "Anonymous public request",
            Selected = true
        }
            ];

            model.Endpoints = EndpointDefinitions.ToList();

            return Task.CompletedTask;
        }

        private string BuildRequestUrl(
            DeveloperPlaygroundEndpointVm endpoint,
            DeveloperPlaygroundViewModel model)
        {
            var baseUrl =
                $"{Request.Scheme}://{Request.Host}/";

            var query = new Dictionary<string, string?>();

            if (endpoint.SupportsCount)
            {
                query["count"] = Math.Clamp(
                        model.Count <= 0 ? 6 : model.Count,
                        1,
                        50)
                    .ToString();
            }

            if (endpoint.SupportsQuery)
            {
                query["q"] = string.IsNullOrWhiteSpace(model.Query)
                    ? null
                    : model.Query.Trim();
            }

            if (endpoint.SupportsPaging)
            {
                query["page"] = Math.Max(
                        model.Page,
                        1)
                    .ToString();

                query["pageSize"] = Math.Clamp(
                        model.PageSize,
                        1,
                        100)
                    .ToString();
            }

            if (endpoint.SupportsPriceRange)
            {
                query["min"] = model.MinimumPrice?.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);

                query["max"] = model.MaximumPrice?.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            }

            var relativeUrl = QueryHelpers.AddQueryString(
                endpoint.Path,
                query
                    .Where(item =>
                        !string.IsNullOrWhiteSpace(item.Value))
                    .ToDictionary(
                        item => item.Key,
                        item => item.Value!));

            return new Uri(
                    new Uri(baseUrl),
                    relativeUrl)
                .ToString();
        }

        private static string FormatJson(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            try
            {
                using var document = JsonDocument.Parse(value);

                return JsonSerializer.Serialize(
                    document.RootElement,
                    PrettyJsonOptions);
            }
            catch (JsonException)
            {
                return value;
            }
        }

        private static IReadOnlyDictionary<string, string>
            ReadResponseHeaders(HttpResponseMessage response)
        {
            return response.Headers
                .Concat(response.Content.Headers)
                .GroupBy(header => header.Key)
                .ToDictionary(
                    group => group.Key,
                    group => string.Join(
                        ", ",
                        group.SelectMany(
                            item => item.Value)));
        }

        private static string BuildCurlExample(string requestUrl)
        {
            return
                $"curl --request GET \\\n" +
                $"  --url \"{requestUrl}\" \\\n" +
                "  --header \"Accept: application/json\"";
        }

        private static string BuildCSharpExample(string requestUrl)
        {
            return
                "using var httpClient = new HttpClient();\n\n" +
                "httpClient.DefaultRequestHeaders.Accept.Add(\n" +
                "    new MediaTypeWithQualityHeaderValue(\"application/json\"));\n\n" +
                $"using var response = await httpClient.GetAsync(\"{requestUrl}\");\n" +
                "response.EnsureSuccessStatusCode();\n\n" +
                "var json = await response.Content.ReadAsStringAsync();";
        }
    }
}