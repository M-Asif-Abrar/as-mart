using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.DTOs
{
    public class ParentCategoryDetailsDto
    {
        [Display(Name = "Id")]
        public int Id { get; set; }

        [Display(Name = "Category Name")]
        public string Name { get; set; } = null!;

        [Display(Name = "Slug")]
        public string Slug { get; set; } = null!;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Child Categories")]
        public int ChildCount { get; set; }

        [Display(Name = "Products in Category")]
        public int ProductCount { get; set; }
    }
}
