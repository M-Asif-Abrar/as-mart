using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.Entities.Marketing
{
    public class MarketingPostingLog
    {
        public int Id { get; set; }

        public int MarketingPostingQueueId { get; set; }
        public MarketingPostingQueue? MarketingPostingQueue { get; set; }

        public MarketingQueueStatus Status { get; set; }

        [MaxLength(2000)]
        public string? Message { get; set; }

        [MaxLength(1000)]
        public string? ScreenshotPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}