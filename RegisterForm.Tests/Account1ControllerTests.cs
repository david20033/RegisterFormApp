using Xunit;
using Moq;
using RegisterForm.Controllers;
using RegisterForm.Services.IServices;
using RegisterForm.ViewModels;
using RegisterForm.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RegisterForm.Tests
{
    public class TestAccountController : AccountController
    {
        public TestAccountController(IAccountService service) : base(service) { }

        protected override string View(string viewName = null, object model = null)
        {
            if (!string.IsNullOrEmpty(viewName))
            {
                return viewName switch
                {
                    "Register.html" => "Register View",
                    "Login.html" => "Login View",
                    "Edit.html" => "Edit View",
                    "ChangePassword.html" => "ChangePassword View",
                    _ => "<h1>View not found</h1>"
                };
            }
            return base.View(viewName, model);
        }
    }

    public class Account1ControllerTests
    {
        private readonly Mock<IAccountService> _mockService;

        public Account1ControllerTests()
        {
            _mockService = new Mock<IAccountService>();
        }

        [Fact]
        public void RegisterGet_UserAuthenticated_Redirects()
        {
            var controller = new TestAccountController(_mockService.Object)
            {
                CurrentUserId = Guid.NewGuid(),
                IsAuthenticated = true
            };

            string result = controller.RegisterGet();
            Assert.Equal("REDIRECT:/Home/Index", result);
        }

        [Fact]
        public void RegisterGet_UserNotAuthenticated_ReturnsViewAndCaptcha()
        {
            _mockService.Setup(s => s.GenerateCaptchaCode()).Returns("123");
            var controller = new TestAccountController(_mockService.Object)
            {
                CurrentUserId = null,
                IsAuthenticated = false
            };

            string result = controller.RegisterGet();
            Assert.Contains("Register", result);
            Assert.Equal("123", controller.Session["CaptchaCode"]);
        }

        [Fact]
        public async Task RegisterPost_Authenticated_Redirects()
        {
            var controller = new TestAccountController(_mockService.Object)
            {
                CurrentUserId = Guid.NewGuid(),
                IsAuthenticated = true
            };
            var model = new RegisterViewModel();
            string result = await controller.RegisterPost(model);
            Assert.Equal("REDIRECT:/Home/Index", result);
        }

        [Fact]
        public async Task RegisterPost_InvalidRegistration_ReturnsViewWithErrors()
        {
            var model = new RegisterViewModel();
            _mockService.Setup(s => s.ValidateRegistrationAsync(model, "captcha"))
                        .ReturnsAsync(new Dictionary<string, string> { { "Email", "Invalid" } });
            _mockService.Setup(s => s.GenerateCaptchaCode()).Returns("newcaptcha");

            var controller = new TestAccountController(_mockService.Object)
            {
                CurrentUserId = null,
                IsAuthenticated = false
            };
            controller.Session["CaptchaCode"] = "captcha";

            string result = await controller.RegisterPost(model);

            Assert.Contains("Register", result);
            Assert.Equal("newcaptcha", controller.Session["CaptchaCode"]);
        }

        [Fact]
        public async Task RegisterPost_ValidRegistration_RedirectsToLogin()
        {
            var model = new RegisterViewModel();
            _mockService.Setup(s => s.ValidateRegistrationAsync(model, "captcha"))
                        .ReturnsAsync(new Dictionary<string, string>());
            _mockService.Setup(s => s.CreateUserAsync(model)).Returns(Task.CompletedTask);
            _mockService.Setup(s => s.GenerateCaptchaCode()).Returns("newcaptcha");

            var controller = new TestAccountController(_mockService.Object)
            {
                CurrentUserId = null,
                IsAuthenticated = false
            };
            controller.Session["CaptchaCode"] = "captcha";

            string result = await controller.RegisterPost(model);
            Assert.Equal("REDIRECT:/Account/Login", result);
            Assert.Equal("newcaptcha", controller.Session["CaptchaCode"]);
        }

        [Fact]
        public void LoginGet_Authenticated_Redirects()
        {
            var controller = new TestAccountController(_mockService.Object)
            {
                CurrentUserId = Guid.NewGuid(),
                IsAuthenticated = true
            };

            string result = controller.LoginGet();
            Assert.Equal("REDIRECT:/Home/Index", result);
        }

        [Fact]
        public void LoginGet_NotAuthenticated_ReturnsView()
        {
            _mockService.Setup(s => s.GenerateCaptchaCode()).Returns("123");
            var controller = new TestAccountController(_mockService.Object)
            {
                CurrentUserId = null,
                IsAuthenticated = false
            };

            string result = controller.LoginGet();
            Assert.Contains("Login", result);
            Assert.Equal("123", controller.Session["CaptchaCode"]);
        }

        [Fact]
        public async Task LoginPost_InvalidModel_ReturnsJson()
        {
            var controller = new TestAccountController(_mockService.Object)
            {
                CurrentUserId = null,
                IsAuthenticated = false
            };

            string result = await controller.LoginPost(null);
            Assert.Contains("Invalid input data", result);
        }

        [Fact]
        public async Task LoginPost_InvalidLogin_ReturnsViewWithCaptcha()
        {
            var model = new LoginViewModel { Email = "a", Password = "b" };
            _mockService.Setup(s => s.ValidateLoginAsync(model, "captcha"))
                        .ReturnsAsync((false, null, "Error"));
            _mockService.Setup(s => s.GenerateCaptchaCode()).Returns("newcaptcha");

            var controller = new TestAccountController(_mockService.Object)
            {
                CurrentUserId = null,
                IsAuthenticated = false
            };
            controller.Session["CaptchaCode"] = "captcha";

            string result = await controller.LoginPost(model);
            Assert.Contains("Login", result);
            Assert.Equal("newcaptcha", controller.Session["CaptchaCode"]);
        }

        [Fact]
        public async Task LoginPost_ValidLogin_Redirects()
        {
            var model = new LoginViewModel { Email = "a", Password = "b" };
            var user = new Users { Id = Guid.NewGuid(), Username = "Test" };
            _mockService.Setup(s => s.ValidateLoginAsync(model, "captcha"))
                        .ReturnsAsync((true, user, null));

            var controller = new TestAccountController(_mockService.Object)
            {
                CurrentUserId = null,
                IsAuthenticated = false
            };

            string result = await controller.LoginPost(model);
            Assert.Equal("REDIRECT:/Home/Index", result); 
            Assert.True(controller.IsAuthenticated);
            Assert.Equal(user.Id, controller.CurrentUserId);
            Assert.Equal(user.Username, controller.CurrentUserUsername);
        }

        [Fact]
        public void Logout_SetsFieldsAndRedirects()
        {
            var controller = new TestAccountController(_mockService.Object)
            {
                CurrentUserId = Guid.NewGuid(),
                CurrentUserUsername = "Test",
                IsAuthenticated = true
            };

            string result = controller.Logout();
            Assert.Equal("REDIRECT:/Home/Index", result);
            Assert.False(controller.IsAuthenticated);
            Assert.Null(controller.CurrentUserId);
            Assert.Equal(string.Empty, controller.CurrentUserUsername);
        }

        [Fact]
        public void CaptchaImage_ReturnsBytes()
        {
            _mockService.Setup(s => s.GenerateCaptchaImage("captcha")).Returns(new byte[] { 1, 2, 3 });

            var controller = new TestAccountController(_mockService.Object)
            {
                CurrentUserId = null
            };
            controller.Session["CaptchaCode"] = "captcha";

            byte[] result = controller.CaptchaImage();
            Assert.Equal(new byte[] { 1, 2, 3 }, result);
        }
    }
}
