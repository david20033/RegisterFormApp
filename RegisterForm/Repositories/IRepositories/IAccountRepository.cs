using RegisterForm.Data;
using RegisterForm.ViewModels;

namespace RegisterForm.Repositories.IRepositories
{
    public interface IAccountRepository
    {
        Task<bool> IsEmailTakenAsync(string email);
        Task<bool> IsUsernameTakenAsync(string username);
        Task<bool> IsPhoneNumberTakenAsync(string phoneNumber);

        Task<Users> CreateUserAsync(RegisterViewModel model);

        Task<Users?> GetUserByEmailAsync(string email);
        Task<Users?> GetUserByIdAsync(Guid Id);
        //Task<bool> VerifyPasswordAsync(Users user, string password);
        Task UpdateUserProfile(Guid UserId, EditProfileViewModel model);
        Task UpdateUserPassword(Guid userId, string password);
    }
}
