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

        /*
         * Existing implementation stores the full API key.
         * A future security hardening step should replace this with ApiKeyHash
         * and ApiKeyPrefix, but we will preserve compatibility in this feature.
         */
        [Required]
        [MaxLength(200)]
        public string ApiKey { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Website { get; set; }

        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        /*
         * Temporary enable/disable state.
         * A disabled key can be enabled again when it is not revoked or expired.
         */
        public bool IsActive { get; set; } = true;

        public int RateLimitPerMinute { get; set; } = 60;

        public int MonthlyQuota { get; set; } = 10_000;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastUsedAt { get; set; }

        /*
         * Key lifecycle fields.
         */
        public DateTime? ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public DateTime? LastRotatedAt { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        /*
         * UI-only lifecycle properties.
         * These are not database columns.
         */
        [NotMapped]
        public bool IsExpired =>
            ExpiresAt.HasValue &&
            ExpiresAt.Value <= DateTime.UtcNow;

        [NotMapped]
        public bool IsRevoked => RevokedAt.HasValue;

        [NotMapped]
        public bool IsUsable =>
            IsActive &&
            !IsRevoked &&
            !IsExpired;

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

                return IsActive ? "Active" : "Disabled";
            }
        }
    }
}