using Xunit;
using RegisterForm.Repositories;
using RegisterForm.ViewModels;
using RegisterForm.Data;
using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;


namespace RegisterForm.Tests
{
    public class AccountRepositorySqlServerTests: IDisposable 
    {
        private readonly string _connectionString;
        private readonly AccountRepository _repo;

        public AccountRepositorySqlServerTests()
        {
            _connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=AccountTestDb;Trusted_Connection=True;";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
                IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
                CREATE TABLE Users (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    FirstName NVARCHAR(100),
                    LastName NVARCHAR(100),
                    Username NVARCHAR(100),
                    Email NVARCHAR(100),
                    Password NVARCHAR(200),
                    PhoneNumber NVARCHAR(20),
                    isEmailConfirmed BIT,
                    DateOfBirth DATETIME
                );", conn);
            cmd.ExecuteNonQuery();

            _repo = new AccountRepository(_connectionString);
        }

        [Fact]
        public async Task CreateUserAsync_CanInsertUser()
        {
            var model = new RegisterViewModel
            {
                FirstName = "John",
                LastName = "Doe",
                Username = "jdoe",
                Email = "jdoe@example.com",
                Password = "Password123",
                PhoneNumber = "1234567890",
                DateOfBirth = DateTime.Today
            };

            await _repo.CreateUserAsync(model);

            var user = await _repo.GetUserByEmailAsync("jdoe@example.com");
            Assert.NotNull(user);
            Assert.Equal("John", user.FirstName);
        }

        [Fact]
        public async Task IsEmailTakenAsync_ReturnsTrueIfExists()
        {
            var model = new RegisterViewModel
            {
                Email = "emailtest@example.com",
                Password = "pass",
                DateOfBirth = new DateTime(2000, 1, 1)
            };
            await _repo.CreateUserAsync(model);

            bool exists = await _repo.IsEmailTakenAsync("emailtest@example.com");
            Assert.True(exists);
        }

        [Fact]
        public async Task UpdateUserProfile_UpdatesFields()
        {
            var model = new RegisterViewModel { FirstName = "Old", LastName = "Name", Username = "u1", Email = "u1@example.com", Password = "pass", DateOfBirth = new DateTime(2000, 1, 1) };
            await _repo.CreateUserAsync(model);
            var user = await _repo.GetUserByEmailAsync("u1@example.com");

            var editModel = new EditProfileViewModel { FirstName = "New", LastName = "User" };
            await _repo.UpdateUserProfile(user.Id, editModel);

            var updated = await _repo.GetUserByIdAsync(user.Id);
            Assert.Equal("New", updated.FirstName);
            Assert.Equal("User", updated.LastName);
        }

        [Fact]
        public async Task UpdateUserPassword_ChangesPassword()
        {
            var model = new RegisterViewModel { Email = "pw@example.com", Password = "oldpass", DateOfBirth = new DateTime(2000, 1, 1) };
            await _repo.CreateUserAsync(model);
            var user = await _repo.GetUserByEmailAsync("pw@example.com");

            await _repo.UpdateUserPassword(user.Id, "newpass");

            var updated = await _repo.GetUserByIdAsync(user.Id);
            Assert.NotEqual(user.Password, updated.Password);
        }

        public void Dispose()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("DROP TABLE IF EXISTS Users;", conn);
            cmd.ExecuteNonQuery();
        }
    }
}
