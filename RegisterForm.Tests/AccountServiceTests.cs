using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RegisterForm.Repositories.IRepositories;
using RegisterForm.Services;
using Moq;
using RegisterForm.ViewModels;
using RegisterForm.Data;
using Microsoft.AspNetCore.Identity;

namespace RegisterForm.Tests
{
    public class AccountServiceTests
    {
        private readonly Mock<IAccountRepository> _repoMock;
        private readonly AccountService _service;
        public AccountServiceTests()
        {
            _repoMock = new Mock<IAccountRepository>();
            _service = new AccountService(_repoMock.Object);
        }
        [Fact]
        public async Task CreateUserAsync_CallsRepositoryAndReturnsUser()
        {
            var model = new RegisterViewModel
            {
                Email = "newuser@test.com",
                Username = "newuser",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };
            var expectedUser = new Users
            {
                Email = "newuser@test.com",
                Username = "newuser"
            };
            _repoMock.Setup(r => r.CreateUserAsync(model)).ReturnsAsync(expectedUser);

            var user = await _service.CreateUserAsync(model);

            Assert.NotNull(user);
            Assert.Equal(expectedUser.Email, user.Email);
            Assert.Equal(expectedUser.Username, user.Username);

            _repoMock.Verify(r => r.CreateUserAsync(model), Times.Once);
        }
        [Fact]
        public async Task ValidateRegistrationAsync_ReturnsCaptchaError_WhenCaptchaIsWrong()
        {
            var model = new RegisterViewModel { Captcha = "abcde" };
            string sessionCaptcha = "zxcvb";

            var errors = await _service.ValidateRegistrationAsync(model, sessionCaptcha);

            Assert.True(errors.ContainsKey("Captcha"));
            Assert.Equal("Invalid CAPTCHA", errors["Captcha"]);
        }
        [Fact]
        public async Task ValidateRegistrationAsync_ReturnsEmailError_WhenEmailIsWrong()
        {
            var model = new RegisterViewModel { Email = "test@example.com" };
            string sessionCaptcha = "zxcvb";

            _repoMock.Setup(r => r.IsEmailTakenAsync("test@example.com"))
                .ReturnsAsync(true);

            var errors = await _service.ValidateRegistrationAsync(model, sessionCaptcha);

            Assert.True(errors.ContainsKey("Email"));
            Assert.Equal("Email is already registered.", errors["Email"]);
        }
        [Fact]
        public async Task ValidateRegistrationAsync_ReturnsUsernameError_WhenUsernameIsWrong()
        {
            var model = new RegisterViewModel { Username = "testuser" };
            string sessionCaptcha = "zxcvb";

            _repoMock.Setup(r => r.IsUsernameTakenAsync("testuser"))
                .ReturnsAsync(true);

            var errors = await _service.ValidateRegistrationAsync(model, sessionCaptcha);

            Assert.True(errors.ContainsKey("Username"));
            Assert.Equal("Username is already taken.", errors["Username"]);

        }
        [Fact]
        public async Task ValidateRegistrationAsync_ReturnsPhoneNumberError_WhenPhoneNumberIsWrong()
        {
            var model = new RegisterViewModel { PhoneNumber = "1234567890" };
            string sessionCaptcha = "zxcvb";

            _repoMock.Setup(r => r.IsPhoneNumberTakenAsync("1234567890"))
                .ReturnsAsync(true);

            var errors = await _service.ValidateRegistrationAsync(model, sessionCaptcha);

            Assert.True(errors.ContainsKey("PhoneNumber"));
            Assert.Equal("Phone number is already in use.", errors["PhoneNumber"]);
        }
        [Fact]
        public async Task ValidateRegistrationAsync_ReturnsMultipleErrors()
        {
            var model = new RegisterViewModel
            {
                Email = "test@mail.com",
                Username = "testuser",
                PhoneNumber = "1234567890",
                Captcha = "abcde"
            };
            string sessionCaptcha = "zxcvb";
            _repoMock.Setup(r => r.IsEmailTakenAsync("test@mail.com")).ReturnsAsync(true);
            _repoMock.Setup(r => r.IsUsernameTakenAsync("testuser")).ReturnsAsync(true);
            _repoMock.Setup(r => r.IsPhoneNumberTakenAsync("1234567890")).ReturnsAsync(true);
            var errors = await _service.ValidateRegistrationAsync(model, sessionCaptcha);

            Assert.Equal(4, errors.Count);
            Assert.True(errors.ContainsKey("Captcha"));
            Assert.True(errors.ContainsKey("Email"));
            Assert.True(errors.ContainsKey("Username"));
            Assert.True(errors.ContainsKey("PhoneNumber"));

        }
        [Fact]
        public async Task ValidateRegistrationAsync_ReturnsNoErrors()
        {
            var model = new RegisterViewModel
            {
                Email = "test@mail.com",
                Username = "testuser",
                PhoneNumber = "1234567890",
                Captcha = "abcde"
            };
            string sessionCaptcha = "abcde";
            _repoMock.Setup(r => r.IsEmailTakenAsync("test@mail.com")).ReturnsAsync(false);
            _repoMock.Setup(r => r.IsUsernameTakenAsync("testuser")).ReturnsAsync(false);
            _repoMock.Setup(r => r.IsPhoneNumberTakenAsync("1234567890")).ReturnsAsync(false);
            var errors = await _service.ValidateRegistrationAsync(model, sessionCaptcha);

            Assert.Empty(errors);
        }
        [Fact]
        public async Task ValidateLoginAsync_ReturnsInvalidLoginAttempt_NullUser_InvalidCaptcha_WhenCaptchaIsWrong()
        {
            var model = new LoginViewModel { Captcha = "abcde" };
            string sessionCaptcha = "zxcvb";
            var result = await _service.ValidateLoginAsync(model, sessionCaptcha);

            Assert.False(result.IsValid);
            Assert.Null(result.User);
            Assert.Equal("Invalid CAPTCHA", result.ErrorMessage);
        }
        [Fact]
        public async Task ValidateLoginAsync_ReturnsInvalidLoginAttempt_NullUser_InvalidLoginAttemptMessage_WhenUserDoesNotExists()
        {
            var model = new LoginViewModel
            {
                Email = "test@mail.com",
                Captcha = "abcde"
            };
            string sessionCaptcha = "abcde";

            _repoMock.Setup(r => r.GetUserByEmailAsync("test@mail.com"))
                .ReturnsAsync((Users?)null);
            var result = await _service.ValidateLoginAsync(model, sessionCaptcha);

            Assert.False(result.IsValid);
            Assert.Null(result.User);
            Assert.Equal("Invalid login attempt.", result.ErrorMessage);
        }
        [Fact]
        public async Task ValidateLoginAsync_ReturnsInvalidLoginAttempt_NullUser_InvalidLoginAttemptMessage_WhenPasswordIsWrong()
        {
            var model = new LoginViewModel
            {
                Email = "test@mail.com",
                Password = "WrongPassword",
                Captcha = "abcde"
            };
            string sessionCaptcha = "abcde";

            var hasher = new PasswordHasher<Users>();
            var correctUser = new Users
            {
                Email = "test@mail.com",
                Password = hasher.HashPassword(null, "CorrectPassword123")
            };
            _repoMock.Setup(r => r.GetUserByEmailAsync("test@mail.com"))
                .ReturnsAsync(correctUser);

            var result = await _service.ValidateLoginAsync(model, sessionCaptcha);

            Assert.False(result.IsValid);
            Assert.Null(result.User);
            Assert.Equal("Invalid login attempt.", result.ErrorMessage);
        }
        [Fact]
        public async Task ValidateLoginAsync_SuccessfulLogin_WhenCredentialsAreCorrect()
        {
            var model = new LoginViewModel
            {
                Email = "test@mail.com",
                Password = "CorrectPassword123",
                Captcha = "abcde"
            };
            string sessionCaptcha = "abcde";
            var hasher = new PasswordHasher<Users>();
            var correctUser = new Users
            {
                Email = "test@mail.com",
                Password = hasher.HashPassword(null, "CorrectPassword123")
            };
            _repoMock.Setup(r => r.GetUserByEmailAsync("test@mail.com"))
                .ReturnsAsync(correctUser);
            var result = await _service.ValidateLoginAsync(model, sessionCaptcha);

            Assert.True(result.IsValid);
            Assert.NotNull(result.User);
            Assert.Null(result.ErrorMessage);
        }
        [Fact]
        public async Task ChangePasswordAsync_ReturnsError_InvalidCaptcha_WhenCaptchaIsWrong()
        {
            var model = new ChangePasswordViewModel
            {
                Id = Guid.NewGuid(),
                OldPassword = "OldPassword123",
                NewPassword = "NewPassword123",
                Captcha = "abcde",
            };
            var sessionCaptcha = "zxcvb";
            var result = await _service.ChangePasswordAsync(model, sessionCaptcha);
            Assert.False(result.Success);
            Assert.Equal("Invalid CAPTCHA", result.ErrorMessage);
        }
        [Fact]
        public async Task ChangePasswordAsync_ReturnsError_UserNotFound_WhenUserIdIsInvalid()
        {
            var model = new ChangePasswordViewModel
            {
                Id = Guid.NewGuid(),
                OldPassword = "OldPassword123",
                NewPassword = "NewPassword123",
                Captcha = "abcde",
            };
            string sessionCaptcha = "abcde";
            _repoMock.Setup(r => r.GetUserByIdAsync(Guid.NewGuid()))
                .ReturnsAsync((Users?)null);
            var result = await _service.ChangePasswordAsync(model, sessionCaptcha);
            Assert.False(result.Success);
            Assert.Equal("User not found.", result.ErrorMessage);
        }
        [Fact]
        public async Task ChangePassword_ReturnsError_NewAndOldPasswordCannotBeTheSame()
        {
            var userId = Guid.NewGuid();
            var model = new ChangePasswordViewModel
            {
                Id = userId,
                OldPassword = "OldPassword123",
                NewPassword = "OldPassword123",
                Captcha = "abcde",
            };
            string sessionCaptcha = "abcde";
            var hasher = new PasswordHasher<Users>();
            var correctUser = new Users
            {
                Id = userId,
                Email = "test@mail.com",
                Password = hasher.HashPassword(null, "OldPassword123")
            };
            _repoMock.Setup(r => r.GetUserByIdAsync(userId))
                .ReturnsAsync(correctUser);
            var result = await _service.ChangePasswordAsync(model, sessionCaptcha);
            Assert.False(result.Success);
            Assert.Equal("New password cannot be the same as the old password.", result.ErrorMessage);
        }
        [Fact]
        public async Task ChangePassword_ReturnsError_WhenUserInputsIncorrectOldPassword()
        {
            var userId = Guid.NewGuid();
            var model = new ChangePasswordViewModel
            {
                Id = userId,
                OldPassword = "IncorrectPassword123",
                NewPassword = "OldPassword123",
                Captcha = "abcde",
            };
            string sessionCaptcha = "abcde";
            var hasher = new PasswordHasher<Users>();
            var correctUser = new Users
            {
                Id = userId,
                Email = "test@mail.com",
                Password = hasher.HashPassword(null, "CorrectPassword123")
            };
            _repoMock.Setup(r => r.GetUserByIdAsync(userId))
                .ReturnsAsync(correctUser);
            var result = await _service.ChangePasswordAsync(model, sessionCaptcha);
            Assert.False(result.Success);
            Assert.Equal("Incorrect old password", result.ErrorMessage);

        }
        [Fact]
        public async Task ChangePassword_UpdateUserPassword_WhenCredentialsAreCorrect()
        {
            var userId = Guid.NewGuid();
            var model = new ChangePasswordViewModel
            {
                Id = userId,
                OldPassword = "newPassword123",
                NewPassword = "OldPassword123",
                Captcha = "abcde",
            };
            string sessionCaptcha = "abcde";
            var hasher = new PasswordHasher<Users>();
            var correctUser = new Users
            {
                Id = userId,
                Email = "test@mail.com",
                Password = hasher.HashPassword(null, "newPassword123")
            };
            _repoMock.Setup(r => r.GetUserByIdAsync(userId))
                .ReturnsAsync(correctUser);
            var result = await _service.ChangePasswordAsync(model, sessionCaptcha);
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
        }
        [Fact]
        public async Task GetUserByIdAsync_ReturnsUser_WhenUserExists()
        {
            var userId = Guid.NewGuid();
            var model = new Users
            {
                Id = userId,
                Email = "test@example.com"
            };
            _repoMock.Setup(r => r.GetUserByIdAsync(userId))
                .ReturnsAsync(model);
            var user = await _service.GetUserByIdAsync(userId);
            Assert.NotNull(user);
            Assert.Equal("test@example.com", user.Email);
        }
        [Fact]
        public async Task GetUserByIdAsync_ReturnsNull_WhenUserDoesNotExists()
        {
            var userId = Guid.NewGuid();
            var user = await _service.GetUserByIdAsync(userId);
            Assert.Null(user);
        }
        [Fact]
        public async Task VerifyPasswordAsync_ReturnsTrue_WhenPasswordIsCorrect()
        {
            var hasher = new PasswordHasher<Users>();
            var hashedPassword = hasher.HashPassword(null, "CorrectPassword123");
            var result = _service.VerifyPasswordAsync(hashedPassword, "CorrectPassword123");
            Assert.True(result);
        }
        [Fact]
        public async Task VerifyPasswordAsync_ReturnsFalse_WhenPasswordIsCorrect()
        {
            var hasher = new PasswordHasher<Users>();
            var hashedPassword = hasher.HashPassword(null, "IncorrectPassword123");
            var result = _service.VerifyPasswordAsync(hashedPassword, "CorrectPassword123");
            Assert.False(result);
        }
        [Fact]
        public async Task UpdateUserProfile_CallsRepositoryWithCorrectParameters()
        {
            var userId = Guid.NewGuid();
            var model = new EditProfileViewModel
            {
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = new DateTime(1990, 1, 1)
            };

            await _service.UpdateUserProfile(userId, model);

            _repoMock.Verify(r => r.UpdateUserProfile(userId, model), Times.Once);
        }
        [Fact]
        public void GenerateCaptchaCode_ReturnsValidCaptcha()
        {
            var captcha = _service.GenerateCaptchaCode();

            Assert.NotNull(captcha);
            Assert.Equal(5, captcha.Length);
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Assert.All(captcha, c => Assert.Contains(c, chars));
        }

        [Fact]
        public void GenerateCaptchaImage_ReturnsValidPngBytes()
        {
            string captchaText = "HELLO";
            var imageBytes = _service.GenerateCaptchaImage(captchaText);
            Assert.NotNull(imageBytes);
            Assert.True(imageBytes.Length > 0);
        }
    }
}
