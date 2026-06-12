using System.Net.Http.Headers;
using System.Text.Json;
using AsMart.Web.Data;
using AsMart.Web.Models.Entities.Marketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AsMart.Web.Services.Marketing
{
    public interface IFacebookPagePublisher
    {
        Task<FacebookPublishResult> PublishQueueItemAsync(int queueItemId);
    }

    public class FacebookPagePublisher : IFacebookPagePublisher
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly FacebookGraphOptions _options;

        public FacebookPagePublisher(
            ApplicationDbContext db,
            IHttpClientFactory httpClientFactory,
            IOptions<FacebookGraphOptions> options)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        public async Task<FacebookPublishResult> PublishQueueItemAsync(int queueItemId)
        {
            var item = await _db.MarketingPostingQueue
                .Include(x => x.SocialTarget)
                .ThenInclude(x => x!.SocialAccount)
                .Include(x => x.MarketingCampaign)
                .FirstOrDefaultAsync(x => x.Id == queueItemId);

            if (item == null)
                return FacebookPublishResult.Fail("Queue item not found.");

            if (item.SocialTarget == null)
                return FacebookPublishResult.Fail("Social target not found.");

            if (item.SocialTarget.TargetType != MarketingTargetType.FacebookPage)
                return FacebookPublishResult.Fail("Only Facebook Page targets can be published through official API.");

            var account = item.SocialTarget.SocialAccount;

            if (account == null)
                return FacebookPublishResult.Fail("Social account not found.");

            if (string.IsNullOrWhiteSpace(account.AccessTokenEncrypted))
                return FacebookPublishResult.Fail("Facebook Page access token is missing.");

            if (string.IsNullOrWhiteSpace(item.SocialTarget.ExternalTargetId))
                return FacebookPublishResult.Fail("Facebook Page ID is missing. Add Page ID in ExternalTargetId.");

            var pageId = item.SocialTarget.ExternalTargetId.Trim();
            var accessToken = account.AccessTokenEncrypted.Trim();

            item.Status = MarketingQueueStatus.Processing;
            item.StartedAt = DateTime.UtcNow;
            item.LastError = null;

            await _db.SaveChangesAsync();

            var result = await PublishTextPostAsync(
                pageId,
                accessToken,
                item.FinalPostText ?? "",
                item.FinalUrlWithUtm);

            if (result.Success)
            {
                item.Status = MarketingQueueStatus.Posted;
                item.PostedAt = DateTime.UtcNow;
                item.PublishedPostUrl = result.PublishedPostUrl;
                item.LastError = null;

                item.SocialTarget.LastPostedAt = DateTime.UtcNow;

                _db.MarketingPostingLogs.Add(new MarketingPostingLog
                {
                    MarketingPostingQueueId = item.Id,
                    Status = MarketingQueueStatus.Posted,
                    Message = "Published to Facebook Page through Graph API.",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                item.Status = MarketingQueueStatus.Failed;
                item.LastError = result.ErrorMessage;
                item.RetryCount += 1;

                _db.MarketingPostingLogs.Add(new MarketingPostingLog
                {
                    MarketingPostingQueueId = item.Id,
                    Status = MarketingQueueStatus.Failed,
                    Message = result.ErrorMessage,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();

            return result;
        }

        private async Task<FacebookPublishResult> PublishTextPostAsync(
            string pageId,
            string accessToken,
            string message,
            string? link)
        {
            var client = _httpClientFactory.CreateClient();

            var endpoint = $"{_options.ApiBaseUrl}/{_options.ApiVersion}/{pageId}/feed";

            var values = new Dictionary<string, string>
            {
                ["message"] = message,
                ["access_token"] = accessToken
            };

            if (!string.IsNullOrWhiteSpace(link))
            {
                values["link"] = link;
            }

            using var content = new FormUrlEncodedContent(values);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await client.PostAsync(endpoint, content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return FacebookPublishResult.Fail(body);

            using var json = JsonDocument.Parse(body);

            var postId = json.RootElement.TryGetProperty("id", out var idProp)
                ? idProp.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(postId))
                return FacebookPublishResult.Fail("Facebook API did not return post id.");

            return FacebookPublishResult.Ok(
                postId,
                $"https://www.facebook.com/{postId}");
        }
    }

    public class FacebookPublishResult
    {
        public bool Success { get; set; }
        public string? FacebookPostId { get; set; }
        public string? PublishedPostUrl { get; set; }
        public string? ErrorMessage { get; set; }

        public static FacebookPublishResult Ok(string facebookPostId, string publishedPostUrl)
        {
            return new FacebookPublishResult
            {
                Success = true,
                FacebookPostId = facebookPostId,
                PublishedPostUrl = publishedPostUrl
            };
        }

        public static FacebookPublishResult Fail(string error)
        {
            return new FacebookPublishResult
            {
                Success = false,
                ErrorMessage = error
            };
        }
    }
}