using System.ComponentModel.DataAnnotations;
using AsMart.Web.Models.Entities.Marketing;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AsMart.Web.Models.ViewModels.Marketing
{
    public class MarketingCampaignCreateViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(180)]
        public string Title { get; set; } = "";

        [MaxLength(220)]
        public string? Slug { get; set; }

        [Required]
        public MarketingCampaignSourceType SourceType { get; set; } = MarketingCampaignSourceType.Custom;

        public int? ProductId { get; set; }

        public int? BlogPostId { get; set; }

        [MaxLength(800)]
        public string? CampaignUrl { get; set; }

        [MaxLength(800)]
        public string? ImageUrl { get; set; }

        [MaxLength(500)]
        public string? ShortDescription { get; set; }

        public DateTime? ScheduledStartAt { get; set; } = DateTime.Now.AddMinutes(15);

        [Range(1, 1440)]
        public int MinDelayMinutes { get; set; } = 20;

        [Range(1, 1440)]
        public int MaxDelayMinutes { get; set; } = 60;

        [MaxLength(120)]
        public string UTMSource { get; set; } = "facebook";

        [MaxLength(120)]
        public string UTMMedium { get; set; } = "group";

        [MaxLength(160)]
        public string? UTMCampaign { get; set; }

        [Required]
        public string CaptionText { get; set; } = "";

        public string? CaptionText2 { get; set; }

        public string? CaptionText3 { get; set; }

        public List<int> SelectedTargetIds { get; set; } = new();

        public List<SelectListItem> ProductOptions { get; set; } = new();
        public List<SelectListItem> BlogPostOptions { get; set; } = new();
        public List<SelectListItem> TargetOptions { get; set; } = new();
    }
}