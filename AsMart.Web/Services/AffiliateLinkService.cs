// Services/AffiliateLinkService.cs
using Microsoft.Extensions.Options;
using System;

namespace AsMart.Web.Services
{
    public class AffiliateLinkService : IAffiliateLinkService
    {
        private readonly AmazonAffiliateOptions _options;

        public AffiliateLinkService(IOptions<AmazonAffiliateOptions> options)
        {
            _options = options.Value;
        }

        public string BuildProductUrl(string asin)
        {
            if (string.IsNullOrWhiteSpace(asin))
                throw new ArgumentException("ASIN is required", nameof(asin));

            var domain = string.IsNullOrWhiteSpace(_options.DefaultDomain)
                ? "www.amazon.com"
                : _options.DefaultDomain.Trim();

            var trackingId = _options.TrackingId?.Trim();

            // Basic URL: https://www.amazon.com/dp/{ASIN}/?tag={TrackingId}
            var baseUrl = $"https://{domain}/dp/{asin.Trim()}";

            if (!string.IsNullOrEmpty(trackingId))
            {
                return $"{baseUrl}/?tag={Uri.EscapeDataString(trackingId)}";
            }

            return baseUrl;
        }
    }
}
