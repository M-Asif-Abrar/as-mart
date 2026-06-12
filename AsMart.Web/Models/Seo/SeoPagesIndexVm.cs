using AsMart.Web.Models.Entities;

namespace AsMart.Web.Models.Seo
{
    public class SeoPagesIndexVm
    {
        public List<SeoPagesIndexRowVm> Items { get; set; } = new();
        public SeoPagesIndexMeta Meta { get; set; } = new();
    }
}
