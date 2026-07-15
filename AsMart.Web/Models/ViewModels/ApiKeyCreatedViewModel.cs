namespace AsMart.Web.Models.ViewModels
{
    public sealed class ApiKeyCreatedViewModel
    {
        public string Title { get; init; } =
            "API key created";

        public string Message { get; init; } =
            string.Empty;

        public string RawApiKey { get; init; } =
            string.Empty;

        public string ClientName { get; init; } =
            string.Empty;

        public DateTime? ExpiresAt { get; init; }

        public string ReturnUrl { get; init; } =
            "/ApiKeys";
    }
}