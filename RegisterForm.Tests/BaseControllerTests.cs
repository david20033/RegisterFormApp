using Xunit;
using Moq;
using RegisterForm.Controllers;
using RegisterForm.Services.IServices;
using System;

namespace RegisterForm.Tests
{

    public class AccountControllerTests
    {
        [Fact]
        public void RegisterGet_UserIsAuthenticated_RedirectsToHomeIndex()
        {
            var mockAccountService = new Mock<IAccountService>();
            var controller = new TestAccountController(mockAccountService.Object)
            {
                CurrentUserId = Guid.NewGuid()
            };
            controller.IsAuthenticated = controller.CurrentUserId != null;

            string result = controller.RegisterGet();

            Assert.Equal("REDIRECT:/Home/Index", result);
        }

        [Fact]
        public void RegisterGet_UserNotAuthenticated_ReturnsRegisterViewAndStoresCaptcha()
        {
            var mockAccountService = new Mock<IAccountService>();
            mockAccountService.Setup(s => s.GenerateCaptchaCode()).Returns("12345");

            var controller = new TestAccountController(mockAccountService.Object)
            {
                CurrentUserId = null
            };
            controller.IsAuthenticated = controller.CurrentUserId != null;

            string result = controller.RegisterGet();

            Assert.Contains("Register", result, StringComparison.OrdinalIgnoreCase);
            Assert.True(controller.Session.ContainsKey("CaptchaCode"));
            Assert.Equal("12345", controller.Session["CaptchaCode"]);
        }
    }
}
