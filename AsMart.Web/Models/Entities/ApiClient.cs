using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsMart.Web.Models.Entities
{
    public class ApiClient
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string ApiKeyHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(24)]
        public string ApiKeyPrefix { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Website { get; set; }

        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        public bool IsActive { get; set; } = true;

        public int RateLimitPerMinute { get; set; } = 60;

        public int MonthlyQuota { get; set; } = 10_000;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastUsedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public DateTime? LastRotatedAt { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [NotMapped]
        public bool IsExpired =>
            ExpiresAt.HasValue &&
            ExpiresAt.Value <= DateTime.UtcNow;

        [NotMapped]
        public bool IsRevoked =>
            RevokedAt.HasValue;

        [NotMapped]
        public bool IsUsable =>
            IsActive &&
            !IsRevoked &&
            !IsExpired;

        [NotMapped]
        public string MaskedApiKey =>
            string.IsNullOrWhiteSpace(ApiKeyPrefix)
            ? "Unavailable"
            : $"{ApiKeyPrefix}••••••••••••••••••••••••";

        [NotMapped]
        public string LifecycleStatus
        {
            get
            {
                if (IsRevoked)
                {
                    return "Revoked";
                }

                if (IsExpired)
                {
                    return "Expired";
                }

                return IsActive
                    ? "Active"
                    : "Disabled";
            }
        }
    }
}