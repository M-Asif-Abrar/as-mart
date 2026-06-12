// Models/Entities/UserProductStatus.cs
using System;

namespace AsMart.Web.Models.Entities
{
    public class UserProductStatus
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public UserProductState State { get; set; }

        // When this state was created (e.g., wishlisted at, marked purchased at)
        public DateTime CreatedAt { get; set; }

        // Optional: you can reuse “Viewed” or “Clicked” multiple times.
        // For “persistent” states (Wishlisted/MarkedPurchased), you might keep only latest record.
        public DateTime? ExpiresAt { get; set; }

        // Optional note (e.g., "Bought for my brother", or Amazon OrderId if user wants to store it)
        public string? Note { get; set; }
    }
}
