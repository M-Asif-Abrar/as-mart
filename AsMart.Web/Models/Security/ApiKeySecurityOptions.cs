using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.Security
{
    public sealed class ApiKeySecurityOptions
    {
        public const string SectionName = "ApiKeySecurity";

        [Required]
        [MinLength(32)]
        public string HashingPepper { get; set; } = string.Empty;

        public bool EnableLegacyBackfill { get; set; }

        public bool ClearLegacyPlaintextAfterBackfill { get; set; }
    }
}