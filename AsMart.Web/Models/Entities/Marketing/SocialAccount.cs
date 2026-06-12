using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.Entities.Marketing
{
    public class SocialAccount
    {
        public int Id { get; set; }

        public int MarketingChannelId { get; set; }
        public MarketingChannel? MarketingChannel { get; set; }

        [Required, MaxLength(120)]
        public string DisplayName { get; set; } = null!;

        [MaxLength(256)]
        public string? ExternalAccountId { get; set; }

        [MaxLength(512)]
        public string? ProfileUrl { get; set; }

        public MarketingPublishMode PublishMode { get; set; } = MarketingPublishMode.Manual;

        public bool IsActive { get; set; } = true;

        public DateTime? TokenExpiresAt { get; set; }

        public string? AccessTokenEncrypted { get; set; }

        public string? RefreshTokenEncrypted { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<SocialTarget> SocialTargets { get; set; } = new List<SocialTarget>();
    }
}