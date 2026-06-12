using System;

namespace AsMart.Web.Models.ViewModels
{
    public class AdminUserListItemViewModel
    {
        public string Id { get; set; } = default!;

        public string? AvatarUrl { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string DisplayName { get; set; } = default!;

        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        public string? City { get; set; }
        public string? Country { get; set; }

        public bool EmailConfirmed { get; set; }
        public bool IsLockedOut { get; set; }

        public bool IsAdmin { get; set; }
    }
}
