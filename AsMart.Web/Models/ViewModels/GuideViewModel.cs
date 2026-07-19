using AsMart.Web.Models.Entities;

namespace AsMart.Web.Models.ViewModels
{
    public class GuideViewModel
    {
        public SeoPage Page { get; set; } = new SeoPage();

        public List<Product> Products { get; set; } = new List<Product>();

        public List<SeoPage> RelatedGuides { get; set; } =
            new List<SeoPage>();
    }
}