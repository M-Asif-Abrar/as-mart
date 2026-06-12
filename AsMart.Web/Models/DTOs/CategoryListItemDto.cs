using System;

namespace AsMart.Web.Models.DTOs
{
    public class CategoryListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? ParentCategoryName { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public int ProductCount { get; set; }
        public int LinksCount { get; set; }
    }
}
