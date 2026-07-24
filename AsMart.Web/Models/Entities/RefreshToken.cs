using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsMart.Web.Models.Entities
{
    [Table("RefreshTokens")]
    public sealed class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime? RevokedAtUtc { get; set; }

        [MaxLength(64)]
        public string? ReplacedByTokenHash { get; set; }

        [MaxLength(64)]
        public string? CreatedByIp { get; set; }

        [MaxLength(64)]
        public string? RevokedByIp { get; set; }

        [MaxLength(512)]
        public string? UserAgent { get; set; }

        public ApplicationUser User { get; set; } = null!;

        [NotMapped]
        public bool IsActive =>
            RevokedAtUtc is null &&
            ExpiresAtUtc > DateTime.UtcNow;
    }
}
