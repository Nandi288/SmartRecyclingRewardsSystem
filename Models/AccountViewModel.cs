using System.ComponentModel.DataAnnotations;

namespace SmartRecyclingRewardsSystem.Models
{
    public class RegisterViewModel
    {
        [Required, StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required, EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [Required(ErrorMessage = "Please select whether you are a Resident or Collection Officer.")]
        [Display(Name = "I am registering as a")]
        public string Role { get; set; }

        [Display(Name = "Receive email notifications")]
        public bool ReceiveEmailNotifications { get; set; }

        [Display(Name = "Receive SMS notifications")]
        public bool ReceiveSmsNotifications { get; set; }

        public RegisterViewModel()
        {
            ReceiveEmailNotifications = true;
        }
    }

    public class LoginViewModel
    {
        [Required, EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}
