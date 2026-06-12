using System.ComponentModel.DataAnnotations;
using AsMart.Web.Models.Entities.Marketing;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AsMart.Web.Models.ViewModels.Marketing
{
    public class MarketingCampaignEditViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(180)]
        public string Title { get; set; } = "";

        [MaxLength(220)]
        public string? Slug { get; set; }

        [Required]
        public MarketingCampaignSourceType SourceType { get; set; }

        public int? ProductId { get; set; }
        public int? BlogPostId { get; set; }

        [MaxLength(800)]
        public string? CampaignUrl { get; set; }

        [MaxLength(800)]
        public string? ImageUrl { get; set; }

        [MaxLength(500)]
        public string? ShortDescription { get; set; }

        public MarketingCampaignStatus Status { get; set; }

        public DateTime? ScheduledStartAt { get; set; }

        [Range(1, 1440)]
        public int MinDelayMinutes { get; set; }

        [Range(1, 1440)]
        public int MaxDelayMinutes { get; set; }

        [MaxLength(120)]
        public string? UTMSource { get; set; }

        [MaxLength(120)]
        public string? UTMMedium { get; set; }

        [MaxLength(160)]
        public string? UTMCampaign { get; set; }

        public List<SelectListItem> ProductOptions { get; set; } = new();
        public List<SelectListItem> BlogPostOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
    }
}