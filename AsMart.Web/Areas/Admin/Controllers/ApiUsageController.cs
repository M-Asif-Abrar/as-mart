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
        private readonly ApplicationDbContext _context;

        public ApiUsageController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.UtcNow.Date;
            var logs = _context.ApiUsageLogs.AsNoTracking();

            var model = new ApiUsageDashboardViewModel
            {
                TotalRequests = await logs.CountAsync(),

                RequestsToday = await logs.CountAsync(x => x.CreatedAt >= today),

                FailedRequestsToday = await logs.CountAsync(x => x.CreatedAt >= today && x.StatusCode >= 400),

                SuccessfulRequestsToday = await logs.CountAsync(x => x.CreatedAt >= today && x.StatusCode >= 200 && x.StatusCode < 400),

                PublicRequests = await logs.CountAsync(x => x.ApiClientId == null),

                ApiKeyRequests = await logs.CountAsync(x => x.ApiClientId != null),

                AverageResponseTimeMs = await logs.AnyAsync()
                    ? await logs.AverageAsync(x => x.ResponseTimeMs)
                    : 0,

                TopEndpoints = await logs
                    .GroupBy(x => x.Endpoint)
                    .Select(g => new ApiEndpointUsageVm
                    {
                        Endpoint = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync(),

                TopClients = await logs
                    .Where(x => x.ApiClientId != null)
                    .GroupBy(x => new
                    {
                        x.ApiClientId,
                        x.ApiClient!.Name,
                        x.ApiClient.LastUsedAt
                    })
                    .Select(g => new ApiClientUsageVm
                    {
                        ClientName = g.Key.Name,
                        LastUsedAt = g.Key.LastUsedAt,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync(),

                StatusSummary = await logs
                    .GroupBy(x => x.StatusCode)
                    .Select(g => new ApiStatusUsageVm
                    {
                        StatusCode = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync(),

                RecentLogs = await logs
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(75)
                    .Select(x => new ApiRecentUsageVm
                    {
                        Endpoint = x.Endpoint,
                        Method = x.HttpMethod,
                        StatusCode = x.StatusCode,
                        ResponseTimeMs = x.ResponseTimeMs,
                        ClientName = x.ApiClient != null ? x.ApiClient.Name : "Public",
                        CreatedAt = x.CreatedAt
                    })
                    .ToListAsync()
            };

            return View(model);
        }
    }
}