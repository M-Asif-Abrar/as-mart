using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using AsMart.Web.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace AsMart.Web.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<IndexModel> _logger;
        private readonly IWebHostEnvironment _environment;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<IndexModel> logger,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _environment = environment;
        }

        public string Username { get; set; } = string.Empty;

        public string? CurrentAvatarUrl { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public class InputModel
        {
            // BASIC
            [Display(Name = "First name")]
            public string? FirstName { get; set; }

            [Display(Name = "Last name")]
            public string? LastName { get; set; }

            [Phone]
            [Display(Name = "Phone number")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "Gender")]
            public string? Gender { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "Date of birth")]
            public DateTime? DateOfBirth { get; set; }

            // ADDRESS
            [Display(Name = "Address line 1")]
            public string? AddressLine1 { get; set; }

            [Display(Name = "Address line 2")]
            public string? AddressLine2 { get; set; }

            [Display(Name = "City")]
            public string? City { get; set; }

            [Display(Name = "State / Province")]
            public string? StateOrProvince { get; set; }

            [Display(Name = "Postal code")]
            public string? PostalCode { get; set; }

            [Display(Name = "Country")]
            public string? Country { get; set; }

            // MARKETING
            [Display(Name = "Receive offers and promotions")]
            public bool IsMarketingOptIn { get; set; }

            // AVATAR
            [Display(Name = "Profile picture")]
            public IFormFile? Avatar { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            Username = user.UserName ?? user.Email ?? "Customer";
            CurrentAvatarUrl = user.AvatarUrl;

            Input = new InputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                AddressLine1 = user.AddressLine1,
                AddressLine2 = user.AddressLine2,
                City = user.City,
                StateOrProvince = user.StateOrProvince,
                PostalCode = user.PostalCode,
                Country = user.Country,
                IsMarketingOptIn = user.IsMarketingOptIn
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // BASIC FIELDS
            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            user.Gender = Input.Gender;
            user.DateOfBirth = Input.DateOfBirth;

            // ADDRESS
            user.AddressLine1 = Input.AddressLine1;
            user.AddressLine2 = Input.AddressLine2;
            user.City = Input.City;
            user.StateOrProvince = Input.StateOrProvince;
            user.PostalCode = Input.PostalCode;
            user.Country = Input.Country;

            // MARKETING
            user.IsMarketingOptIn = Input.IsMarketingOptIn;

            // PHONE VIA USERMANAGER (so security stamps etc are correct)
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            // AVATAR UPLOAD
            if (Input.Avatar != null && Input.Avatar.Length > 0)
            {
                var uploadsRootFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploadsRootFolder);

                // Simple extension check – you can harden this further
                var extension = Path.GetExtension(Input.Avatar.FileName);
                if (string.IsNullOrEmpty(extension))
                {
                    extension = ".jpg";
                }

                var fileName = $"{user.Id}{extension}";
                var filePath = Path.Combine(uploadsRootFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.Avatar.CopyToAsync(stream);
                }

                user.AvatarUrl = $"/uploads/avatars/{fileName}";
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Error updating user profile for user {UserId}", user.Id);
                StatusMessage = "Unexpected error when trying to save your profile.";
                return RedirectToPage();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated.";
            return RedirectToPage();
        }
    }
}
