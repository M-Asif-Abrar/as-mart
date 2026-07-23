using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.ViewModels
{
    public sealed class DeveloperApplicationEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Application name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        [Url]
        public string? Website { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}