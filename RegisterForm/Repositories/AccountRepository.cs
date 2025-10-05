
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RegisterForm.Data;
using RegisterForm.Repositories.IRepositories;
using RegisterForm.ViewModels;

namespace RegisterForm.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly RegisterFormDbContext _context;
        public AccountRepository(RegisterFormDbContext context)
        {
            _context = context;
        }

        public async Task<Users> CreateUserAsync(RegisterViewModel model)
        {
            var user = Users.MapFromRegisterViewModel(model);
            user.Password = new PasswordHasher<Users>().HashPassword(user, model.Password);
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<Users?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<Users?> GetUserByIdAsync(Guid Id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == Id);
        }

        public async Task<bool> IsEmailTakenAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            return user != null;
        }

        public async Task<bool> IsPhoneNumberTakenAsync(string phoneNumber)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            return user != null;
        }

        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            return user != null;
        }

        public async Task UpdateUserProfile(Guid UserId, EditProfileViewModel model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == UserId);
            if (user != null)
            {
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }
        }
        public async Task UpdateUserPassword(Guid userId, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.Password = new PasswordHasher<Users>().HashPassword(user, password);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }
        }

    }
}
