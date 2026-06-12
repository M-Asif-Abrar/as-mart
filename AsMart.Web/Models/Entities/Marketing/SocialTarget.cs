using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.Entities.Marketing
{
    public class SocialTarget
    {
        public int Id { get; set; }

        public int SocialAccountId { get; set; }
        public SocialAccount? SocialAccount { get; set; }

        public MarketingTargetType TargetType { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(512)]
        public string? TargetUrl { get; set; }

        [MaxLength(256)]
        public string? ExternalTargetId { get; set; }

        [MaxLength(120)]
        public string? Niche { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastPostedAt { get; set; }

        public int DailyPostLimit { get; set; } = 1;

        public int MinDelayMinutes { get; set; } = 20;

        public int MaxDelayMinutes { get; set; } = 60;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}