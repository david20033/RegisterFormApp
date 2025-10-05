using System.ComponentModel.DataAnnotations;

namespace RegisterForm.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(255, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; } = false;

        [Required(ErrorMessage = "Captcha is required")]
        public string Captcha { get; set; } = string.Empty;
    }
}
