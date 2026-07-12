using AsMart.Web.Data;
using AsMart.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ApiUsageController : Controller
    {
        private static readonly int[] AllowedPageSizes = { 25, 50, 100 };

        private readonly ApplicationDbContext _context;

        public ApiUsageController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int? clientId,
            string? endpoint,
            int? statusCode,
            DateTime? from,
            DateTime? to,
            string requestType = "all",
            long? minimumResponseTimeMs = null,
            int page = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(page, 1);

            if (!AllowedPageSizes.Contains(pageSize))
            {
                pageSize = 25;
            }

            requestType = NormalizeRequestType(requestType);
            endpoint = string.IsNullOrWhiteSpace(endpoint)
                ? null
                : endpoint.Trim();

            if (minimumResponseTimeMs < 0)
            {
                minimumResponseTimeMs = 0;
            }

            if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The From date cannot be later than the To date.");

                (from, to) = (to, from);
            }

            var todayUtc = DateTime.UtcNow.Date;

            var allLogs = _context.ApiUsageLogs
                .AsNoTracking();

            // EF Core DbContext instances are not thread-safe.
            // Execute every query sequentially on this scoped context.
            var totalRequests =
                await allLogs.CountAsync(cancellationToken);

            var requestsToday =
                await allLogs.CountAsync(
                    x => x.CreatedAt >= todayUtc,
                    cancellationToken);

            var failedRequestsToday =
                await allLogs.CountAsync(
                    x =>
                        x.CreatedAt >= todayUtc &&
                        x.StatusCode >= 400,
                    cancellationToken);

            var successfulRequestsToday =
                await allLogs.CountAsync(
                    x =>
                        x.CreatedAt >= todayUtc &&
                        x.StatusCode >= 200 &&
                        x.StatusCode < 400,
                    cancellationToken);

            var publicRequests =
                await allLogs.CountAsync(
                    x => x.ApiClientId == null,
                    cancellationToken);

            var apiKeyRequests =
                await allLogs.CountAsync(
                    x => x.ApiClientId != null,
                    cancellationToken);

            var averageResponseTimeMs =
                await allLogs
                    .Select(x => (double?)x.ResponseTimeMs)
                    .AverageAsync(cancellationToken)
                ?? 0;

            var topEndpoints =
                await allLogs
                    .GroupBy(x => x.Endpoint)
                    .Select(group => new ApiEndpointUsageVm
                    {
                        Endpoint = group.Key,
                        Count = group.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Endpoint)
                    .Take(10)
                    .ToListAsync(cancellationToken);

            var topClients =
                await allLogs
                    .Where(x => x.ApiClientId != null)
                    .GroupBy(x => new
                    {
                        x.ApiClientId,
                        ClientName = x.ApiClient!.Name,
                        WebsiteUrl = x.ApiClient.Website,
                        x.ApiClient.LastUsedAt
                    })
                    .Select(group => new ApiClientUsageVm
                    {
                        ClientName = group.Key.ClientName,
                        WebsiteUrl = group.Key.WebsiteUrl,
                        LastUsedAt = group.Key.LastUsedAt,
                        Count = group.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.ClientName)
                    .Take(10)
                    .ToListAsync(cancellationToken);

            var statusSummary =
                await allLogs
                    .GroupBy(x => x.StatusCode)
                    .Select(group => new ApiStatusUsageVm
                    {
                        StatusCode = group.Key,
                        Count = group.Count()
                    })
                    .OrderBy(x => x.StatusCode)
                    .ToListAsync(cancellationToken);

            var clientOptions =
                await _context.ApiClients
                    .AsNoTracking()
                    .Include(x => x.User)
                    .OrderBy(x => x.Name)
                    .ThenBy(x => x.Id)
                    .Select(x => new ApiClientFilterOptionVm
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Email = x.User != null
                            ? x.User.Email
                            : null
                    })
                    .ToListAsync(cancellationToken);

            var filteredQuery = ApplyFilters(
                allLogs,
                clientId,
                endpoint,
                statusCode,
                from,
                to,
                requestType,
                minimumResponseTimeMs);

            var totalFilteredRequests =
                await filteredQuery.CountAsync(cancellationToken);

            var totalPages = totalFilteredRequests == 0
                ? 1
                : (int)Math.Ceiling(
                    totalFilteredRequests / (double)pageSize);

            if (page > totalPages)
            {
                page = totalPages;
            }

            var recentLogs = await filteredQuery
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ApiRecentUsageVm
                {
                    Id = x.Id,
                    Endpoint = x.Endpoint,
                    QueryString = x.QueryString,
                    Method = x.HttpMethod,
                    StatusCode = x.StatusCode,
                    ResponseTimeMs = x.ResponseTimeMs,
                    ApiClientId = x.ApiClientId,
                    ClientName = x.ApiClient != null
                        ? x.ApiClient.Name
                        : "Public",
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var model = new ApiUsageDashboardViewModel
            {
                TotalRequests = totalRequests,
                RequestsToday = requestsToday,
                FailedRequestsToday = failedRequestsToday,
                SuccessfulRequestsToday = successfulRequestsToday,
                PublicRequests = publicRequests,
                ApiKeyRequests = apiKeyRequests,
                AverageResponseTimeMs = averageResponseTimeMs,

                TopEndpoints = topEndpoints,
                TopClients = topClients,
                StatusSummary = statusSummary,
                ClientOptions = clientOptions,

                ClientId = clientId,
                Endpoint = endpoint,
                StatusCode = statusCode,
                From = from?.Date,
                To = to?.Date,
                RequestType = requestType,
                MinimumResponseTimeMs = minimumResponseTimeMs,

                Page = page,
                PageSize = pageSize,
                TotalFilteredRequests = totalFilteredRequests,
                TotalPages = totalPages,
                RecentLogs = recentLogs
            };

            return View(model);
        }

        private static IQueryable<AsMart.Web.Models.ApiUsageLog> ApplyFilters(
            IQueryable<AsMart.Web.Models.ApiUsageLog> query,
            int? clientId,
            string? endpoint,
            int? statusCode,
            DateTime? from,
            DateTime? to,
            string requestType,
            long? minimumResponseTimeMs)
        {
            if (clientId.HasValue)
            {
                query = query.Where(
                    x => x.ApiClientId == clientId.Value);
            }

            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                query = query.Where(
                    x => x.Endpoint.Contains(endpoint));
            }

            if (statusCode.HasValue)
            {
                query = query.Where(
                    x => x.StatusCode == statusCode.Value);
            }

            if (from.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(
                    from.Value.Date,
                    DateTimeKind.Utc);

                query = query.Where(
                    x => x.CreatedAt >= fromUtc);
            }

            if (to.HasValue)
            {
                var toExclusiveUtc = DateTime.SpecifyKind(
                    to.Value.Date.AddDays(1),
                    DateTimeKind.Utc);

                query = query.Where(
                    x => x.CreatedAt < toExclusiveUtc);
            }

            if (string.Equals(
                requestType,
                "public",
                StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(
                    x => x.ApiClientId == null);
            }
            else if (string.Equals(
                requestType,
                "apikey",
                StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(
                    x => x.ApiClientId != null);
            }

            if (minimumResponseTimeMs.HasValue)
            {
                query = query.Where(
                    x =>
                        x.ResponseTimeMs >=
                        minimumResponseTimeMs.Value);
            }

            return query;
        }

        private static string NormalizeRequestType(
            string? requestType)
        {
            return requestType?.Trim().ToLowerInvariant() switch
            {
                "public" => "public",
                "apikey" => "apikey",
                _ => "all"
            };
        }
    }
}
