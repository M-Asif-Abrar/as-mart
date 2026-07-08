// Services/AffiliateLinkService.cs
using Microsoft.Extensions.Options;

namespace AsMart.Web.Services
{
    public class AffiliateLinkService : IAffiliateLinkService
    {
        private readonly AmazonAffiliateOptions _options;

        public AffiliateLinkService(IOptions<AmazonAffiliateOptions> options)
        {
            _options = options.Value;
        }

        public string BuildProductUrl(string? asin)
        {
            if (string.IsNullOrWhiteSpace(asin))
                return "#";

            var domain = string.IsNullOrWhiteSpace(_options.DefaultDomain)
                ? "www.amazon.com"
                : _options.DefaultDomain.Trim();

            var trackingId = _options.TrackingId?.Trim();

            var baseUrl = $"https://{domain}/dp/{asin.Trim()}";

            if (!string.IsNullOrWhiteSpace(trackingId))
                return $"{baseUrl}/?tag={Uri.EscapeDataString(trackingId)}";

            return baseUrl;
        }
    }
}