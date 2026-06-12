using System;

namespace AsMart.Web.Models.Entities
{
    public class BlogPostRating
    {
        public int Id { get; set; }

        public int BlogPostId { get; set; }
        public BlogPost BlogPost { get; set; } = null!;

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        // 1–5 stars
        public byte Value { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
