namespace AsMart.Web.Middleware
{
    public sealed class LegacyApiVersionMiddleware
    {
        private readonly RequestDelegate _next;

        public LegacyApiVersionMiddleware(
            RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context)
        {
            var path = context.Request.Path;

            if (!path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var isVersionedRoute =
                path.StartsWithSegments("/api/v1");

            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-API-Version"] =
                    isVersionedRoute
                        ? "1"
                        : "legacy";

                if (!isVersionedRoute)
                {
                    context.Response.Headers["Deprecation"] =
                        "true";

                    context.Response.Headers["Sunset"] =
                        "Wed, 31 Dec 2028 23:59:59 GMT";

                    context.Response.Headers.Append(
                        "Link",
                        "</api/v1>; rel=\"successor-version\"");
                }

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}