using RegisterForm.Data;
using RegisterForm.ViewModels;

namespace RegisterForm.Services.IServices
{
    public interface IAccountService
    {
        Task<Users> CreateUserAsync(RegisterViewModel model);
        Task<Dictionary<string, string>> ValidateRegistrationAsync(RegisterViewModel model, string sessionCaptcha);
        Task<(bool IsValid, Users? User, string? ErrorMessage)> ValidateLoginAsync(LoginViewModel model, string sessionCaptcha);
        Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(ChangePasswordViewModel model, string sessionCaptcha);

        Task<Users?> GetUserByIdAsync(Guid Id);
        bool VerifyPasswordAsync(string userPassword, string inputPassword);
        EditProfileViewModel MapToEditProfileViewModel(Users user);

        Task UpdateUserProfile(Guid UserId, EditProfileViewModel model);
        string GenerateCaptchaCode();
        byte[] GenerateCaptchaImage(string text);
    }
}
