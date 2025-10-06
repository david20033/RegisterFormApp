
using Microsoft.Data.SqlClient;
using RegisterForm.Data;
using RegisterForm.Helpers;
using RegisterForm.Repositories.IRepositories;
using RegisterForm.ViewModels;

namespace RegisterForm.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly string _connectionString;
        public AccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task CreateUserAsync(RegisterViewModel user)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string sql = @"
                INSERT INTO Users (Id,FirstName, LastName, Username, Email, Password, PhoneNumber,isEmailConfirmed, DateOfBirth)
                VALUES (@Id,@FirstName, @LastName, @Username, @Email, @Password, @PhoneNumber, @isEmailConfirmed, @DateOfBirth)";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    string hashedPassword = PasswordHelper.HashPassword(user.Password);
                    cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", user.LastName);
                    cmd.Parameters.AddWithValue("@Username", user.Username);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);
                    cmd.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
                    cmd.Parameters.AddWithValue("@isEmailConfirmed", false);
                    cmd.Parameters.AddWithValue("@DateOfBirth", user.DateOfBirth);

                    await cmd.ExecuteNonQueryAsync();
                }
            }

        }

        public async Task<Users?> GetUserByEmailAsync(string email)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "SELECT Id, Username, Email, Password, FirstName, LastName, PhoneNumber, DateOfBirth FROM Users WHERE Email = @Email";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Email", email);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Users
                {
                    Id = reader.GetGuid(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    Password = reader.GetString(3),
                    FirstName = reader.GetString(4),
                    LastName = reader.GetString(5),
                    PhoneNumber = reader.GetString(6),
                    DateOfBirth = reader.GetDateTime(7),
                };
            }

            return null;
        }
        public async Task<Users?> GetUserByIdAsync(Guid id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "SELECT Id, Username, Email, Password, FirstName, LastName, PhoneNumber, DateOfBirth FROM Users WHERE Id = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id.ToString().ToUpper());

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Users
                {
                    Id = reader.GetGuid(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    Password = reader.GetString(3),
                    FirstName = reader.GetString(4),
                    LastName = reader.GetString(5),
                    PhoneNumber = reader.GetString(6),
                    DateOfBirth = reader.GetDateTime(7),
                };
            }

            return null;
        }
        public async Task<bool> IsEmailTakenAsync(string email)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Email", email);

            int count = (int)await cmd.ExecuteScalarAsync();
            return count > 0;
        }


        public async Task<bool> IsPhoneNumberTakenAsync(string phoneNumber)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "SELECT COUNT(1) FROM Users WHERE PhoneNumber = @PhoneNumber";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PhoneNumber", phoneNumber);

            int count = (int)await cmd.ExecuteScalarAsync();
            return count > 0;
        }


        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "SELECT COUNT(1) FROM Users WHERE Username = @Username";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Username", username);

            int count = (int)await cmd.ExecuteScalarAsync();
            return count > 0;
        }


        public async Task UpdateUserProfile(Guid userId, EditProfileViewModel model)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"UPDATE Users SET FirstName = @FirstName, LastName = @LastName WHERE Id = @Id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FirstName", model.FirstName);
            cmd.Parameters.AddWithValue("@LastName", model.LastName);
            cmd.Parameters.AddWithValue("@Id", userId);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateUserPassword(Guid userId, string password)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var hashedPassword=PasswordHelper.HashPassword(password);

            string sql = "UPDATE Users SET Password = @Password WHERE Id = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Password", hashedPassword);
            cmd.Parameters.AddWithValue("@Id", userId);

            await cmd.ExecuteNonQueryAsync();
        }


    }
}
