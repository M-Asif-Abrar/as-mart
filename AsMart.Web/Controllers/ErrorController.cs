using AsMart.Web.Models.ViewModels;
using AsMart.Web.Services.Repositories.ErrorPages;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;


namespace AsMart.Web.Controllers
{
    [DisableRateLimiting]
    [Route("error")]
    public sealed class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;
        private readonly ErrorLogRepository _repo;

        public ErrorController(ILogger<ErrorController> logger, ErrorLogRepository repo)
        {
            _logger = logger;
            _repo = repo;
        }

        [HttpGet("{code:int}")]
        public async Task<IActionResult> Status(int code)
        {
            var vm = BuildViewModel(code);

            if (code == StatusCodes.Status404NotFound)
            {
                var reExec = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

                var originalPath = reExec?.OriginalPath ?? HttpContext.Request.Path.Value ?? "";
                var originalQuery = reExec?.OriginalQueryString ?? HttpContext.Request.QueryString.Value ?? "";

                var fullUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.PathBase}{originalPath}{originalQuery}";

                var referrer = Request.Headers.Referer.ToString();
                var ua = Request.Headers.UserAgent.ToString();

                await _repo.Log404Async(
                    path: originalPath,
                    fullUrl: fullUrl,
                    referrer: string.IsNullOrWhiteSpace(referrer) ? null : referrer,
                    userAgent: string.IsNullOrWhiteSpace(ua) ? null : ua
                );
            }

            if (code == StatusCodes.Status500InternalServerError)
            {
                var exFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
                var ex = exFeature?.Error;

                vm.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
                vm.ShowRequestId = true;

                if (ex != null)
                {
                    _logger.LogError(ex, "Unhandled exception. TraceId={TraceId}", vm.RequestId);
                }
                else
                {
                    _logger.LogError("500 error without exception feature. TraceId={TraceId}", vm.RequestId);
                }
            }

            Response.StatusCode = code;

            return code switch
            {
                400 => View("400", vm),
                401 => View("401", vm),
                403 => View("403", vm),
                404 => View("404", vm),
                410 => View("410", vm),
                429 => View("429", vm),
                500 => View("500", vm),
                503 => View("503", vm),
                _ => View("400", vm)
            };
        }

        private static ErrorPageViewModel BuildViewModel(int code)
        {
            var links = new List<QuickLink>
            {
                new() { Title = "Best Binoculars", Url = "/guides/best-binoculars" },
                new() { Title = "Drone Cameras", Url = "/category/drone-cameras" },
                new() { Title = "Night Vision", Url = "/category/night-vision" },
                new() { Title = "Smart Home", Url = "/category/smart-home" }
            };

            return code switch
            {
                400 => new ErrorPageViewModel
                {
                    StatusCode = 400,
                    Title = "Bad Request",
                    Message = "That request doesn’t look valid.",
                    Hint = "Try removing unusual parameters and retry. If you came from a link, it may be outdated.",
                    QuickLinks = links
                },
                401 => new ErrorPageViewModel
                {
                    StatusCode = 401,
                    Title = "Sign in required",
                    Message = "You need to sign in to access this page.",
                    Hint = "If you believe this is a mistake, contact the administrator.",
                    QuickLinks = links
                },
                403 => new ErrorPageViewModel
                {
                    StatusCode = 403,
                    Title = "Access denied",
                    Message = "You don’t have permission to view this page.",
                    Hint = "If you think you should have access, sign in with a different account or contact the administrator.",
                    QuickLinks = links
                },
                404 => new ErrorPageViewModel
                {
                    StatusCode = 404,
                    Title = "Page not found",
                    Message = "We couldn’t find the page you’re looking for.",
                    Hint = "Check the URL, or search for the product/guide below.",
                    QuickLinks = links
                },
                410 => new ErrorPageViewModel
                {
                    StatusCode = 410,
                    Title = "This page is gone",
                    Message = "This content was permanently removed.",
                    Hint = "Try a related guide or search for newer recommendations.",
                    QuickLinks = links
                },
                429 => new ErrorPageViewModel
                {
                    StatusCode = 429,
                    Title = "Too many requests",
                    Message = "You’re sending requests too quickly.",
                    Hint = "Wait a moment and try again.",
                    QuickLinks = links
                },
                500 => new ErrorPageViewModel
                {
                    StatusCode = 500,
                    Title = "Something went wrong",
                    Message = "An unexpected error occurred on our side.",
                    Hint = "Try again in a moment. If it keeps happening, share the request id below.",
                    QuickLinks = links
                },
                503 => new ErrorPageViewModel
                {
                    StatusCode = 503,
                    Title = "We’ll be right back",
                    Message = "As-Mart is temporarily unavailable for maintenance.",
                    Hint = "Please retry shortly.",
                    QuickLinks = links
                },
                _ => new ErrorPageViewModel
                {
                    StatusCode = 400,
                    Title = "Bad Request",
                    Message = "That request doesn’t look valid.",
                    Hint = "Please try again.",
                    QuickLinks = links
                }
            };
        }
    }
}
