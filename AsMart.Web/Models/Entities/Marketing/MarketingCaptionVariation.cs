using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.Entities.Marketing
{
    public class MarketingCaptionVariation
    {
        public int Id { get; set; }

        public int MarketingCampaignId { get; set; }
        public MarketingCampaign? MarketingCampaign { get; set; }

        [Required]
        public string CaptionText { get; set; } = null!;

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}