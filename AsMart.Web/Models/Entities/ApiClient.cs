using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.Entities
{
    public class ApiClient
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = "";

        [Required, MaxLength(200)]
        public string ApiKey { get; set; } = "";

        [MaxLength(300)]
        public string? Website { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public bool IsActive { get; set; } = true;

        public int RateLimitPerMinute { get; set; } = 60;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUsedAt { get; set; }

        public int MonthlyQuota { get; set; } = 10000;
    }
}