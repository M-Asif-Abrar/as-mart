// /Redirects/DbRedirectMiddleware.cs
using AsMart.Web.Services.Repositories.Redirects;
using Microsoft.AspNetCore.Http;

namespace AsMart.Web.Services.Repositories.Redirects
{
    public sealed class DbRedirectMiddleware
    {
        private readonly RequestDelegate _next;

        public DbRedirectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, RedirectRuleRepository repo)
        {
            if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value ?? "/";
            var normalized = RedirectRuleRepository.NormalizePath(path);

            if (normalized.StartsWith("/error", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var rule = await repo.FindAsync(normalized);
            if (rule == null)
            {
                await _next(context);
                return;
            }

            await repo.TrackHitAsync(rule.Id);

            context.Response.StatusCode = rule.StatusCode == 302 ? StatusCodes.Status302Found : StatusCodes.Status301MovedPermanently;
            context.Response.Headers.Location = rule.ToUrl;
        }
    }
}
