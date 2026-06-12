using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.Entities.Marketing
{
    public class MarketingChannel
    {
        public int Id { get; set; }

        [Required, MaxLength(80)]
        public string Name { get; set; } = null!;

        public MarketingPlatform Platform { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<SocialAccount> SocialAccounts { get; set; } = new List<SocialAccount>();
    }
}