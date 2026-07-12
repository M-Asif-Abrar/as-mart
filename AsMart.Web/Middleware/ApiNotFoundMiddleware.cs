using AsMart.Web.Models.Api;
using AsMart.Web.Services;

namespace AsMart.Web.Middleware
{
    /// <summary>
    /// Converts unmatched /api routes into the standard JSON error format.
    /// Existing controller-generated response bodies are preserved.
    /// </summary>
    public sealed class ApiNotFoundMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiNotFoundMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                return;
            }

            if (context.Response.HasStarted)
            {
                return;
            }

            if (context.Response.StatusCode !=
                StatusCodes.Status404NotFound)
            {
                return;
            }

            /*
             * If an endpoint matched, its controller/action owns the 404.
             * This middleware handles only completely unmatched API routes.
             */
            if (context.GetEndpoint() is not null)
            {
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode =
                StatusCodes.Status404NotFound;
            context.Response.ContentType =
                "application/json; charset=utf-8";
            context.Response.Headers.CacheControl = "no-store";

            var response = ApiResponseFactory.Error(
                ApiErrorCodes.NotFound,
                "The requested API endpoint was not found.",
                context.TraceIdentifier);

            await context.Response.WriteAsJsonAsync(
                response,
                context.RequestAborted);
        }
    }
}