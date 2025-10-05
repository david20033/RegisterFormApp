using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.AspNetCore.Identity;
using RegisterForm.Data;
using RegisterForm.Repositories.IRepositories;
using RegisterForm.Services.IServices;
using RegisterForm.ViewModels;
using static System.Net.Mime.MediaTypeNames;


namespace RegisterForm.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        public AccountService(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<Users> CreateUserAsync(RegisterViewModel model)
        {
            return await _accountRepository.CreateUserAsync(model);
        }

        public async Task<Users?> GetUserByIdAsync(Guid Id)
        {
            return await _accountRepository.GetUserByIdAsync(Id);
        }
        public async Task<Dictionary<string, string>> ValidateRegistrationAsync(RegisterViewModel model, string sessionCaptcha)
        {
            var errors = new Dictionary<string, string>();

            if (!string.Equals(model.Captcha, sessionCaptcha, StringComparison.OrdinalIgnoreCase))
                errors.Add("Captcha", "Invalid CAPTCHA");

            if (await _accountRepository.IsEmailTakenAsync(model.Email))
                errors.Add("Email", "Email is already registered.");

            if (await _accountRepository.IsUsernameTakenAsync(model.Username))
                errors.Add("Username", "Username is already taken.");

            if (await _accountRepository.IsPhoneNumberTakenAsync(model.PhoneNumber))
                errors.Add("PhoneNumber", "Phone number is already in use.");

            return errors;
        }
        public async Task<(bool IsValid, Users? User, string? ErrorMessage)> ValidateLoginAsync(LoginViewModel model, string sessionCaptcha)
        {
            if (!string.Equals(model.Captcha, sessionCaptcha, StringComparison.OrdinalIgnoreCase))
                return (false, null, "Invalid CAPTCHA");

            var user = await _accountRepository.GetUserByEmailAsync(model.Email);
            if (user == null || !VerifyPasswordAsync(user.Password, model.Password))
                return (false, null, "Invalid login attempt.");

            return (true, user, null);
        }
        public async Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(ChangePasswordViewModel model, string sessionCaptcha)
        {
            if (string.IsNullOrEmpty(model.Captcha) || !string.Equals(model.Captcha, sessionCaptcha, StringComparison.OrdinalIgnoreCase))
                return (false, "Invalid CAPTCHA");

            var user = await GetUserByIdAsync(model.Id);
            if (user == null)
                return (false, "User not found.");

            if ( VerifyPasswordAsync(user.Password, model.NewPassword))
                return (false, "New password cannot be the same as the old password.");

            if (! VerifyPasswordAsync(user.Password, model.OldPassword))
                return (false, "Incorrect old password");

            await _accountRepository.UpdateUserPassword(model.Id, model.NewPassword);
            return (true, null);
        }

        public bool VerifyPasswordAsync(string userPassword, string inputPassword)
        {
            var passwordHasher = new PasswordHasher<object>();
            var result = passwordHasher.VerifyHashedPassword(null, userPassword, inputPassword);
            if (result == PasswordVerificationResult.Success)
            {
                return true;
            }
            return false;
        }
        public EditProfileViewModel MapToEditProfileViewModel(Users user)
        {
            return new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth
            };
        }
        public async Task UpdateUserProfile(Guid UserId, EditProfileViewModel model)
        {
            await _accountRepository.UpdateUserProfile(UserId, model);
        }

        public string GenerateCaptchaCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 5)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public byte[] GenerateCaptchaImage(string text)
        {

            using var bitmap = new Bitmap(120, 40);
            using var g = Graphics.FromImage(bitmap);

            g.Clear(Color.LightGray);
            var font = new System.Drawing.Font("Arial", 20, FontStyle.Bold);
            g.DrawString(text, font, Brushes.Black, 10, 5);
            //g.DrawLine(new Pen(Color.Red,3), 120, 0, 0, 30);
            g.DrawLine(new Pen(Color.Blue, 2), 120, 0, 0, 50);
            Random rnd = new Random();
            g.DrawLine(new Pen(Color.Blue, 2), 120, rnd.Next(0,120), 0, 0);
            g.DrawLine(new Pen(Color.Yellow, 2), rnd.Next(0, 120), 0, 0, 50);
            g.DrawLine(new Pen(Color.Green, 2), 120, 0, rnd.Next(0, 120), 50);
            g.DrawLine(new Pen(Color.Black, 2), 120, 0, 0, rnd.Next(0, 120));

            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);

            return ms.ToArray();
        }
        
    }
}
