// Models/Entities/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace AsMart.Web.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        // Basic profile
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        // Display name convenience (not mapped as a column)
        public string DisplayName =>
            string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
                ? (UserName ?? Email ?? "Customer")
                : $"{FirstName} {LastName}".Trim();

        // Avatar / profile photo (store relative path or URL)
        // e.g. "/uploads/avatars/user123.jpg"
        public string? AvatarUrl { get; set; }

        // Optional demographic info
        public string? Gender { get; set; }          // "Male", "Female", "Other" or your own codes
        public DateTime? DateOfBirth { get; set; }

        // Primary address (good enough for now for a mart / store profile)
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? StateOrProvince { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }

        // Marketing / communication preferences
        public bool IsMarketingOptIn { get; set; }

        // Existing navigation properties
        public ICollection<UserProductStatus> ProductStatuses { get; set; }
            = new List<UserProductStatus>();

        public ICollection<ClickLog> ClickLogs { get; set; }
            = new List<ClickLog>();

        public ICollection<ApiClient> ApiClients { get; set; } = new List<ApiClient>();
    }
}
