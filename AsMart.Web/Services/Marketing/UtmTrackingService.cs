using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Services.Marketing
{
    public interface IUtmTrackingService
    {
        Task TrackVisitAsync(
            HttpContext httpContext,
            int? productId = null,
            int? blogPostId = null,
            string clickType = "SocialCampaignVisit");
    }

    public class UtmTrackingService : IUtmTrackingService
    {
        private readonly ApplicationDbContext _db;

        public UtmTrackingService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task TrackVisitAsync(
            HttpContext httpContext,
            int? productId = null,
            int? blogPostId = null,
            string clickType = "SocialCampaignVisit")
        {
            var request = httpContext.Request;

            var ua = request.Headers["User-Agent"].ToString();

            if (IsBotLike(ua))
                return;

            var ip = httpContext.Connection.RemoteIpAddress?.ToString();

            var since = DateTime.UtcNow.AddMinutes(-10);

            var landingUrl =
                $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";

            var utmSource = request.Query["utm_source"].ToString();
            var utmMedium = request.Query["utm_medium"].ToString();
            var utmCampaign = request.Query["utm_campaign"].ToString();
            var utmContent = request.Query["utm_content"].ToString();
            var utmTerm = request.Query["utm_term"].ToString();

            var already = await _db.ClickLogs.AnyAsync(x =>
                x.ProductId == productId &&
                x.BlogPostId == blogPostId &&
                x.ClickType == clickType &&
                x.IPAddress == ip &&
                x.UserAgent == ua &&
                x.ClickedAt >= since);

            if (already)
                return;

            var referrer = request.Headers["Referer"].ToString();

            var log = new ClickLog
            {
                ProductId = productId,
                BlogPostId = blogPostId,

                ClickType = clickType,

                UtmSource = utmSource,
                UtmMedium = utmMedium,
                UtmCampaign = utmCampaign,
                UtmContent = utmContent,
                UtmTerm = utmTerm,

                ReferrerUrl = referrer,
                LandingUrl = landingUrl,

                ClickedAt = DateTime.UtcNow,

                IPAddress = ip,
                UserAgent = ua,

                IsSocialTraffic =
                    !string.IsNullOrWhiteSpace(utmSource),

                IsFacebookTraffic =
                    utmSource.Contains("facebook", StringComparison.OrdinalIgnoreCase),

                IsInstagramTraffic =
                    utmSource.Contains("instagram", StringComparison.OrdinalIgnoreCase),

                IsTelegramTraffic =
                    utmSource.Contains("telegram", StringComparison.OrdinalIgnoreCase),

                IsPinterestTraffic =
                    utmSource.Contains("pinterest", StringComparison.OrdinalIgnoreCase)
            };

            await TryResolveCampaignAsync(log);

            _db.ClickLogs.Add(log);

            await _db.SaveChangesAsync();
        }

        private async Task TryResolveCampaignAsync(ClickLog log)
        {
            if (string.IsNullOrWhiteSpace(log.UtmCampaign))
                return;

            var campaign = await _db.MarketingCampaigns
                .FirstOrDefaultAsync(x =>
                    x.Slug == log.UtmCampaign ||
                    x.Title == log.UtmCampaign);

            if (campaign != null)
            {
                log.MarketingCampaignId = campaign.Id;
            }
        }

        private static bool IsBotLike(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                return true;

            var ua = userAgent.ToLowerInvariant();

            return ua.Contains("bot") ||
                   ua.Contains("crawler") ||
                   ua.Contains("spider") ||
                   ua.Contains("slurp") ||
                   ua.Contains("headless") ||
                   ua.Contains("lighthouse") ||
                   ua.Contains("curl") ||
                   ua.Contains("wget");
        }
    }
}