using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.Security
{
    public sealed class JwtOptions
    {
        public const string SectionName = "Jwt";

        [Required]
        [MinLength(32)]
        public string SigningKey { get; set; } = string.Empty;

        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        [Range(5, 1440)]
        public int AccessTokenMinutes { get; set; } = 30;

        [Range(1, 365)]
        public int RefreshTokenDays { get; set; } = 30;
    }
}