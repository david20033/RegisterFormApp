using Xunit;
using System.Threading.Tasks;
using RegisterForm.Data;
using Microsoft.EntityFrameworkCore;
using RegisterForm.Repositories;
using RegisterForm.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace RegisterForm.Tests
{
    public class AccountRepositoryTests
    {
        private RegisterFormDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<RegisterFormDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new RegisterFormDbContext(options);
        }
        [Fact]
        public async Task IsEmailTakenAsync_ReturnsTrue_WhenEmailExists()
        {
            var context = GetDbContext("TestDb1");
            context.Users.Add(new Users { Email = "test@example.com" });
            await context.SaveChangesAsync();
            var repo=new AccountRepository(context);

            var result = await repo.IsEmailTakenAsync("test@example.com");

            Assert.True(result);
        }
        [Fact]
        public async Task IsEmailTakenAsync_ReturnsFalse_WhenEmailDoesNotExists()
        {
            var context = GetDbContext("TestDb2");
            var repo = new AccountRepository(context);

            var result = await repo.IsEmailTakenAsync("test@example.com");

            Assert.False(result);
        }
        [Fact]
        public async Task IsUsernameTakenAsync_ReturnsTrue_WhenUsernameExists()
        {
            var context = GetDbContext("TestDb3");
            context.Users.Add(new Users { Username = "testuser" });
            await context.SaveChangesAsync();
            var repo = new AccountRepository(context);

            var result = await repo.IsUsernameTakenAsync("testuser");

            Assert.True(result);
        }
        [Fact]
        public async Task IsUsernameTakenAsync_ReturnsFalse_WhenUsernameDoesNotExists()
        {
            var context = GetDbContext("TestDb4");
            var repo = new AccountRepository(context);

            var result = await repo.IsUsernameTakenAsync("testuser");
            Assert.False(result);
        }
        [Fact]
        public async Task IsPhoneNumberTakenAsync_ReturnsTrue_WhenPhoneNumberExists()
        {
            var context = GetDbContext("TestDb5");
            context.Users.Add(new Users { PhoneNumber = "1234567890" });
            await context.SaveChangesAsync();
            var repo = new AccountRepository(context);
            var result = await repo.IsPhoneNumberTakenAsync("1234567890");
            Assert.True(result);
        }
        [Fact]
        public async Task IsPhoneNumberTakenAsync_ReturnsFalse_WhenPhoneNumberDoesNotExists()
        {
            var context = GetDbContext("TestDb6");
            var repo = new AccountRepository(context);
            var result = await repo.IsPhoneNumberTakenAsync("1234567890");
            Assert.False(result);
        }
        [Fact]
        public async Task CreateUserAsync_ShouldSaveUserWithHashPassword()
        {
            var context = GetDbContext("TestDb7");
            var repo = new AccountRepository(context);

            var model = new RegisterViewModel
            {
                Email = "test@example.com",
                Username = "ExampleUsername",
                Password = "SecurePassword123!",
            };
            var RepoUser = await repo.CreateUserAsync(model);

            Assert.NotNull(RepoUser);
            Assert.Equal("test@example.com", RepoUser.Email);
            Assert.Equal("ExampleUsername", RepoUser.Username);
            Assert.NotEqual("SecurePassword123!", RepoUser.Password);
            var DbUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
            Assert.NotNull(DbUser);
            Assert.Equal(DbUser.Id, RepoUser.Id);
        }
        [Fact]
        public async Task GetUserByEmailAsync_ReturnsUserWhenExist()
        {
            var context = GetDbContext("TestDb8");
            var repo = new AccountRepository(context);
            context.Add(new Users { Email="text@example.com", Username = "testuser" });
            await context.SaveChangesAsync();

            var user = await repo.GetUserByEmailAsync("text@example.com");

            Assert.NotNull(user);
            Assert.Equal("text@example.com", user.Email);
            Assert.Equal("testuser", user.Username);
        }
        [Fact]
        public async Task GetUserByEmailAsync_ReturnsNullWhenDoesNotExist()
        {
            var context = GetDbContext("TestDb9");
            var repo = new AccountRepository(context);
            var user = await repo.GetUserByEmailAsync("text@example.com");
            Assert.Null(user);
        }
        [Fact]
        public async Task GetUserByIdAsync_ReturnUserWhenExists()
        {
            var context = GetDbContext("TestDb10");
            var repo = new AccountRepository(context);
            Guid UserId= Guid.NewGuid();
            context.Add(new Users { Id = UserId, Email = "test@example.com" });
            context.SaveChanges();

            var user = await repo.GetUserByIdAsync(UserId);
            Assert.NotNull(user);
            Assert.Equal(UserId,user.Id);
            Assert.Equal("test@example.com", user.Email);
        }
        [Fact]
        public async Task GetUserByIdAsync_ReturnsNullWhenDoesNotExist()
        {
            var context = GetDbContext("TestDb11");
            var repo = new AccountRepository(context);
            var user = await repo.GetUserByIdAsync(Guid.NewGuid());
            Assert.Null(user);
        }
        [Fact]
        public async Task UpdateUserProfile_ShouldUpdateUserDetails()
        {
            var context = GetDbContext("TestDb12");
            var repo = new AccountRepository(context);
            Guid UserId = Guid.NewGuid();
            context.Users.Add(new Users
            {
                Id = UserId,
                FirstName = "OldFirstName",
                LastName = "OldLastName",
                PhoneNumber = "1234567890"
            });
            await context.SaveChangesAsync();
            var model = new EditProfileViewModel
            {
                FirstName = "NewFirstName",
                LastName = "NewLastName",
                PhoneNumber = "0987654321"
            };
            await repo.UpdateUserProfile(UserId, model);

            var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == UserId);
            Assert.NotNull(updatedUser);
            Assert.Equal("NewFirstName", updatedUser.FirstName);
            Assert.Equal("NewLastName", updatedUser.LastName);
            Assert.Equal("1234567890", updatedUser.PhoneNumber);
        }
        [Fact]
        public async Task UpdateUserProfile_DoesNothing_WhenUserDoesNotExist()
        {
            var context = GetDbContext("TestDb13");
            var repo = new AccountRepository(context);
            var model = new EditProfileViewModel
            {
                FirstName = "NewFirstName",
                LastName = "NewLastName",
                PhoneNumber = "0987654321"
            };
            await repo.UpdateUserProfile(Guid.NewGuid(), model);
            var usersCount = await context.Users.CountAsync();
            Assert.Equal(0, usersCount);
        }
        [Fact]
        public async Task UpdateUserPassword_ShouldHashAndUpdatePassword()
        {
            var context = GetDbContext("TestDb14");
            var repo = new AccountRepository(context);
            Guid UserId = Guid.NewGuid();
            context.Users.Add(new Users
            {
                Id = UserId,
                Password = "OldHashedPassword"
            });
            await context.SaveChangesAsync();
            await repo.UpdateUserPassword(UserId, "NewSecurePassword123!");
            var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == UserId);
            var hasher = new PasswordHasher<Users>();
            var verify = hasher.VerifyHashedPassword(updatedUser, updatedUser.Password, "NewSecurePassword123!");
            Assert.NotNull(updatedUser);
            Assert.NotEqual("OldHashedPassword", updatedUser.Password);
            Assert.NotEqual("NewSecurePassword123!", updatedUser.Password);
            Assert.Equal(PasswordVerificationResult.Success, verify);
        }
        [Fact]
        public async Task UpdateUserPassword_DoesNothing_WhenUserDoesNotExist()
        {
            var context = GetDbContext("TestDb15");
            var repo = new AccountRepository(context);
            await repo.UpdateUserPassword(Guid.NewGuid(), "NewSecurePassword123!");
            var usersCount = await context.Users.CountAsync();
            Assert.Equal(0, usersCount);
        }
    }
}