// Services/AmazonAffiliateOptions.cs
namespace AsMart.Web.Services
{
    public class AmazonAffiliateOptions
    {
        public string DefaultDomain { get; set; } = "www.amazon.com";   // amazon.com, amazon.co.uk, etc.
        public string TrackingId { get; set; } = string.Empty;          // e.g., as-mart-20
    }
}
