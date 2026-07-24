namespace AsMart.Web.Models.Api.Auth
{
    public sealed record AuthApiResponse<T>(
        bool Success,
        T? Data,
        AuthApiError? Error,
        string TraceId)
    {
        public static AuthApiResponse<T> Ok(T data, string traceId) =>
            new(true, data, null, traceId);

        public static AuthApiResponse<T> Fail(
            string code,
            string message,
            string traceId) =>
            new(false, default, new AuthApiError(code, message), traceId);
    }

    public sealed record AuthApiError(string Code, string Message);
}
