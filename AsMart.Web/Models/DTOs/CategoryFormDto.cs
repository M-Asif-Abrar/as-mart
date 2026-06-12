using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.DTOs
{
    public class CategoryFormDto
    {
        public int? Id { get; set; }  // null for create

        [Required]
        [StringLength(200)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = null!;

        [Display(Name = "Slug")]
        public string? Slug { get; set; }  // optional visible, but we will generate if empty

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Parent Category")]
        public int? ParentCategoryId { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "SEO Badges (one per line)")]
        public string? Links { get; set; }
    }
}
