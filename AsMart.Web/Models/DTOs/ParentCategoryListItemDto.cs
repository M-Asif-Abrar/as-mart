using System;

namespace AsMart.Web.Models.DTOs
{
    public class ParentCategoryListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public int ChildCount { get; set; }
        public int ProductCount { get; set; }
    }
}
