// Models/Entities/Collection.cs
using System.Collections.Generic;

namespace AsMart.Web.Models.Entities
{
    public class Collection
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<CollectionProduct> CollectionProducts { get; set; } = new List<CollectionProduct>();
    }

    public class CollectionProduct
    {
        public int CollectionId { get; set; }
        public Collection Collection { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
