namespace AsMart.Web.Models.ViewModels
{
    public class NavbarChildCategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }

    public class NavbarParentCategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        public List<NavbarChildCategoryViewModel> Children { get; set; } = new();
    }
}
