using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.Api.Auth
{
    public sealed class RefreshTokenRequestDto
    {
        [Required]
        [MaxLength(2048)]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
