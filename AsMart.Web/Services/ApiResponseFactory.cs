using AsMart.Web.Models.Api;

namespace AsMart.Web.Services
{
    public static class ApiResponseFactory
    {
        public static ApiResponse<T> Success<T>(
            T data,
            string message,
            object? meta = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Meta = meta
            };
        }

        public static ApiResponse Success(string message)
        {
            return new ApiResponse
            {
                Success = true,
                Message = message
            };
        }

        public static ApiResponse Error(
            string error,
            string message,
            string? traceId = null,
            IReadOnlyDictionary<string, string[]>? errors = null)
        {
            return new ApiResponse
            {
                Success = false,
                Error = error,
                Message = message,
                TraceId = traceId,
                Errors = errors
            };
        }

        public static ApiResponse<T> Error<T>(
            string error,
            string message,
            string? traceId = null,
            T? data = default,
            object? meta = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Error = error,
                Message = message,
                TraceId = traceId,
                Data = data,
                Meta = meta
            };
        }
    }
}