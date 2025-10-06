using Xunit;
using Moq;
using RegisterForm.Services;
using RegisterForm.Repositories.IRepositories;
using RegisterForm.ViewModels;
using RegisterForm.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RegisterForm.Helpers;

namespace RegisterForm.Tests
{
    public class AccountServiceTests
    {
        private readonly Mock<IAccountRepository> _mockRepo;
        private readonly AccountService _service;

        public AccountServiceTests()
        {
            _mockRepo = new Mock<IAccountRepository>();
            _service = new AccountService(_mockRepo.Object);
        }

        [Fact]
        public async Task CreateUserAsync_CallsRepository()
        {
            var model = new RegisterViewModel();
            await _service.CreateUserAsync(model);
            _mockRepo.Verify(r => r.CreateUserAsync(model), Times.Once);
        }

        [Fact]
        public async Task GetUserByIdAsync_ReturnsUser()
        {
            var user = new Users { Id = Guid.NewGuid() };
            _mockRepo.Setup(r => r.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            var result = await _service.GetUserByIdAsync(user.Id);
            Assert.Equal(user, result);
        }

        [Fact]
        public async Task ValidateRegistrationAsync_ReturnsErrors()
        {
            var model = new RegisterViewModel
            {
                Email = "a@a.com",
                Username = "user",
                PhoneNumber = "123",
                Captcha = "wrong"
            };
            _mockRepo.Setup(r => r.IsEmailTakenAsync(model.Email)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.IsUsernameTakenAsync(model.Username)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.IsPhoneNumberTakenAsync(model.PhoneNumber)).ReturnsAsync(true);

            var errors = await _service.ValidateRegistrationAsync(model, "captcha");
            Assert.Contains("Captcha", errors.Keys);
            Assert.Contains("Email", errors.Keys);
            Assert.Contains("Username", errors.Keys);
            Assert.Contains("PhoneNumber", errors.Keys);
        }

        [Fact]
        public async Task ValidateLoginAsync_InvalidCaptcha_ReturnsFalse()
        {
            var model = new LoginViewModel { Email = "a", Password = "b", Captcha = "x" };
            var result = await _service.ValidateLoginAsync(model, "y");
            Assert.False(result.IsValid);
            Assert.Null(result.User);
            Assert.Equal("Invalid CAPTCHA", result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateLoginAsync_InvalidUser_ReturnsFalse()
        {
            var model = new LoginViewModel { Email = "a", Password = "b", Captcha = "x" };
            _mockRepo.Setup(r => r.GetUserByEmailAsync(model.Email)).ReturnsAsync((Users)null);
            var result = await _service.ValidateLoginAsync(model, "x");
            Assert.False(result.IsValid);
            Assert.Null(result.User);
            Assert.Equal("Invalid login attempt.", result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateLoginAsync_ValidUser_ReturnsTrue()
        {
            var model = new LoginViewModel { Email = "a", Password = "b", Captcha = "x" };
            var user = new Users { Password = PasswordHelper.HashPassword("b") };
            _mockRepo.Setup(r => r.GetUserByEmailAsync(model.Email)).ReturnsAsync(user);

            var result = await _service.ValidateLoginAsync(model, "x");
            Assert.True(result.IsValid);
            Assert.Equal(user, result.User);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public async Task ChangePasswordAsync_InvalidCaptcha_ReturnsFalse()
        {
            var model = new ChangePasswordViewModel { Captcha = "x" };
            var result = await _service.ChangePasswordAsync(model, "y");
            Assert.False(result.Success);
            Assert.Equal("Invalid CAPTCHA", result.ErrorMessage);
        }

        [Fact]
        public async Task ChangePasswordAsync_UserNotFound_ReturnsFalse()
        {
            var model = new ChangePasswordViewModel { Captcha = "x", Id = Guid.NewGuid() };
            var result = await _service.ChangePasswordAsync(model, "x");
            Assert.False(result.Success);
            Assert.Equal("User not found.", result.ErrorMessage);
        }

        [Fact]
        public async Task ChangePasswordAsync_NewPasswordSameAsOld_ReturnsFalse()
        {
            var user = new Users { Password = PasswordHelper.HashPassword("same") };
            _mockRepo.Setup(r => r.GetUserByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

            var model = new ChangePasswordViewModel { Captcha = "x", Id = Guid.NewGuid(), OldPassword = "same", NewPassword = "same" };
            var result = await _service.ChangePasswordAsync(model, "x");
            Assert.False(result.Success);
            Assert.Equal("New password cannot be the same as the old password.", result.ErrorMessage);
        }

        [Fact]
        public async Task ChangePasswordAsync_WrongOldPassword_ReturnsFalse()
        {
            var user = new Users { Password = PasswordHelper.HashPassword("correct") };
            _mockRepo.Setup(r => r.GetUserByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

            var model = new ChangePasswordViewModel { Captcha = "x", Id = Guid.NewGuid(), OldPassword = "wrong", NewPassword = "newpass" };
            var result = await _service.ChangePasswordAsync(model, "x");
            Assert.False(result.Success);
            Assert.Equal("Incorrect old password", result.ErrorMessage);
        }

        [Fact]
        public async Task ChangePasswordAsync_ValidChanges_CallsRepository()
        {
            var user = new Users { Password = PasswordHelper.HashPassword("oldpass") };
            _mockRepo.Setup(r => r.GetUserByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);
            _mockRepo.Setup(r => r.UpdateUserPassword(It.IsAny<Guid>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var model = new ChangePasswordViewModel { Captcha = "x", Id = Guid.NewGuid(), OldPassword = "oldpass", NewPassword = "newpass" };
            var result = await _service.ChangePasswordAsync(model, "x");
            Assert.True(result.Success);
            _mockRepo.Verify(r => r.UpdateUserPassword(model.Id, model.NewPassword), Times.Once);
        }

        [Fact]
        public void VerifyPasswordAsync_ReturnsTrueForValid()
        {
            var hash = PasswordHelper.HashPassword("secret");
            Assert.True(_service.VerifyPasswordAsync(hash, "secret"));
        }

        [Fact]
        public void VerifyPasswordAsync_ReturnsFalseForInvalid()
        {
            var hash = PasswordHelper.HashPassword("secret");
            Assert.False(_service.VerifyPasswordAsync(hash, "wrong"));
        }

        [Fact]
        public void MapToEditProfileViewModel_MapsCorrectly()
        {
            var user = new Users
            {
                FirstName = "A",
                LastName = "B",
                Username = "C",
                Email = "a@b.com",
                PhoneNumber = "123",
                DateOfBirth = DateTime.Today
            };
            var model = _service.MapToEditProfileViewModel(user);
            Assert.Equal(user.FirstName, model.FirstName);
            Assert.Equal(user.LastName, model.LastName);
            Assert.Equal(user.Username, model.Username);
            Assert.Equal(user.Email, model.Email);
            Assert.Equal(user.PhoneNumber, model.PhoneNumber);
            Assert.Equal(user.DateOfBirth, model.DateOfBirth);
        }

        [Fact]
        public async Task UpdateUserProfile_CallsRepository()
        {
            var model = new EditProfileViewModel();
            var userId = Guid.NewGuid();
            await _service.UpdateUserProfile(userId, model);
            _mockRepo.Verify(r => r.UpdateUserProfile(userId, model), Times.Once);
        }

        [Fact]
        public void GenerateCaptchaCode_Returns5CharacterString()
        {
            var code = _service.GenerateCaptchaCode();
            Assert.Equal(5, code.Length);
        }

        [Fact]
        public void GenerateCaptchaImage_ReturnsBytes()
        {
            var bytes = _service.GenerateCaptchaImage("TEST");
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
        }
    }
}
