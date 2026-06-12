using AsMart.Web.Data;
using AsMart.Web.Models.Entities.Marketing;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Services.Marketing
{
    public class MarketingQueueAutoPublisherWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MarketingQueueAutoPublisherWorker> _logger;

        public MarketingQueueAutoPublisherWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<MarketingQueueAutoPublisherWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessScheduledFacebookPagePostsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Marketing queue auto publisher failed.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessScheduledFacebookPagePostsAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IFacebookPagePublisher>();

            var now = DateTime.Now;

            var queueIds = await db.MarketingPostingQueue
                .AsNoTracking()
                .Include(x => x.SocialTarget)
                .Where(x =>
                    x.Status == MarketingQueueStatus.Scheduled &&
                    x.ScheduledAt != null &&
                    x.ScheduledAt <= now &&
                    x.SocialTarget != null &&
                    x.SocialTarget.TargetType == MarketingTargetType.FacebookPage)
                .OrderBy(x => x.ScheduledAt)
                .Take(5)
                .Select(x => x.Id)
                .ToListAsync(stoppingToken);

            foreach (var queueId in queueIds)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                var result = await publisher.PublishQueueItemAsync(queueId);

                if (result.Success)
                {
                    _logger.LogInformation("Facebook queue item {QueueId} published successfully.", queueId);
                }
                else
                {
                    _logger.LogWarning("Facebook queue item {QueueId} failed: {Error}", queueId, result.ErrorMessage);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}