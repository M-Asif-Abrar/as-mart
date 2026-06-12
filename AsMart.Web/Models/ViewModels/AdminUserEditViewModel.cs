using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models.ViewModels
{
    public class AdminUserEditViewModel
    {
        public string Id { get; set; } = default!;

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        public string? City { get; set; }

        public string? Country { get; set; }

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Admin User")]
        public bool IsAdmin { get; set; }

        [Display(Name = "Lock Account")]
        public bool IsLockedOut { get; set; }
    }
}