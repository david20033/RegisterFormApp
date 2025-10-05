using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RegisterForm.ViewModels;
using System.ComponentModel.DataAnnotations;
namespace RegisterForm.Data
{
    public class Users
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        [Required]
        [DataType(DataType.Password)]
        [StringLength(255, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
        [Required]
        [StringLength(50, MinimumLength = 5)]
        public string Username { get; set; } = string.Empty;
        [Required]
        public DateTime DateOfBirth { get; set; }
        [Phone]
        [StringLength(10)]
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsEmailConfirmed { get; set; } = false;

        public static Users MapFromRegisterViewModel(RegisterViewModel model)
        {
            var passwordHasher = new PasswordHasher<object>();
            string plainPassword = model.Password;
            string hashedPassword = passwordHasher.HashPassword(null,plainPassword);
            return new Users
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Username = model.Username,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = model.DateOfBirth,
                Password = hashedPassword
            };
        }
    }
}
