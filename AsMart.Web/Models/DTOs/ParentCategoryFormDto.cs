using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.DTOs
{
    public class ParentCategoryFormDto
    {
        public int? Id { get; set; }  // null for create

        [Required]
        [StringLength(200)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = null!;

        [Display(Name = "Slug")]
        public string? Slug { get; set; }   // optional; auto-generate if empty

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
