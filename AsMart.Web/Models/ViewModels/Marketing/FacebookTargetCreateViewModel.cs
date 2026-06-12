using System.ComponentModel.DataAnnotations;
using AsMart.Web.Models.Entities.Marketing;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AsMart.Web.Models.ViewModels.Marketing
{
    public class FacebookTargetCreateViewModel
    {
        public int Id { get; set; }

        [Required]
        public int SocialAccountId { get; set; }

        [Required]
        public MarketingTargetType TargetType { get; set; } = MarketingTargetType.FacebookGroup;

        [Required, MaxLength(200)]
        public string Name { get; set; } = "";

        [MaxLength(512)]
        public string? TargetUrl { get; set; }

        [MaxLength(256)]
        public string? ExternalTargetId { get; set; }

        [MaxLength(120)]
        public string? Niche { get; set; }

        public bool IsActive { get; set; } = true;

        [Range(1, 50)]
        public int DailyPostLimit { get; set; } = 1;

        [Range(1, 1440)]
        public int MinDelayMinutes { get; set; } = 20;

        [Range(1, 1440)]
        public int MaxDelayMinutes { get; set; } = 60;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public List<SelectListItem> SocialAccountOptions { get; set; } = new();
    }
}