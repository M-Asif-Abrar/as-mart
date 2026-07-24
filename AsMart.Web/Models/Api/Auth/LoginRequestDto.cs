using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.Api.Auth
{
    public sealed class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;
    }
}
