// Models/Entities/Tag.cs
using System.Collections.Generic;

namespace AsMart.Web.Models.Entities
{
    public class Tag
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;

        public ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();
    }

    public class ProductTag
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}
