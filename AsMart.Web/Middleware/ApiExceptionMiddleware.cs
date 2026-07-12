using AsMart.Web.Models.Api;
using AsMart.Web.Services;

namespace AsMart.Web.Middleware
{
    public sealed class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionMiddleware> _logger;

        public ApiExceptionMiddleware(
            RequestDelegate next,
            ILogger<ApiExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException)
                when (context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "API request cancelled. Method: {Method}, Path: {Path}, TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);
            }
            catch (Exception exception)
            {
                if (!context.Request.Path.StartsWithSegments("/api"))
                {
                    throw;
                }

                _logger.LogError(
                    exception,
                    "Unhandled API exception. Method: {Method}, Path: {Path}, TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);

                if (context.Response.HasStarted)
                {
                    throw;
                }

                context.Response.Clear();

                context.Response.StatusCode =
                    StatusCodes.Status500InternalServerError;

                context.Response.ContentType =
                    "application/json; charset=utf-8";

                context.Response.Headers.CacheControl = "no-store";

                var response = ApiResponseFactory.Error(
                    ApiErrorCodes.ServerError,
                    "An unexpected server error occurred.",
                    context.TraceIdentifier);

                await context.Response.WriteAsJsonAsync(
                    response,
                    context.RequestAborted);
            }
        }
    }
}