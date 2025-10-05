using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Newtonsoft.Json.Linq;
using RegisterForm.Controllers;
using RegisterForm.Data;
using RegisterForm.Repositories.IRepositories;
using RegisterForm.Services;
using RegisterForm.Services.IServices;
using RegisterForm.Tests.Helpers;
using RegisterForm.ViewModels;

namespace RegisterForm.Tests
{
    public class AccountControllerTests
    {
        private readonly Mock<IAccountService> _accountServiceMock;
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            _accountServiceMock = new Mock<IAccountService>();
            _authServiceMock = new Mock<IAuthService>();

            _controller = new AccountController(_accountServiceMock.Object, _authServiceMock.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public void Register_ReturnsView_AndSetsCaptchaInSession()
        {
            _accountServiceMock.Setup(s => s.GenerateCaptchaCode())
                              .Returns("ABCDE"); 


            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession(); 
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var result = _controller.Register();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("ABCDE", httpContext.Session.GetString("CaptchaCode"));

            _accountServiceMock.Verify(s => s.GenerateCaptchaCode(), Times.Once);
        }
        [Fact]
        public void Register_Get_RedirectsToHome_WhenUserIsAuthenticated()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "testuser")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = principal,
                Session = new TestSession()
            };

            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "1"));

            var result = _controller.Register();

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }
        [Fact]
        public async Task Register_Post_ReturnsView_WhenModelStateIsInvalid()
        {
            _controller.ControllerContext.HttpContext.Session = new TestSession();
            _controller.ModelState.AddModelError("Email", "Required");

            var model = new RegisterViewModel();

            var result = await _controller.Register(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public async Task Register_Post_ReturnsView_WhenValidationFails()
        {
            _controller.ControllerContext.HttpContext.Session = new TestSession();
            _controller.HttpContext.Session.SetString("CaptchaCode", "ABCDE");

            var model = new RegisterViewModel { Email = "test@mail.com", Captcha = "wrong" };

            _accountServiceMock
                .Setup(s => s.ValidateRegistrationAsync(model, "ABCDE"))
                .ReturnsAsync(new Dictionary<string, string> { { "Captcha", "Invalid CAPTCHA" } });

            _accountServiceMock
                .Setup(s => s.GenerateCaptchaCode())
                .Returns("NEWCD");

            var result = await _controller.Register(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.Equal("NEWCD", _controller.HttpContext.Session.GetString("CaptchaCode"));
            Assert.True(_controller.ModelState.ContainsKey("Captcha"));
        }

        [Fact]
        public async Task Register_Post_RedirectsToLogin_WhenModelIsValid()
        {
            _controller.ControllerContext.HttpContext.Session = new TestSession();
            _controller.HttpContext.Session.SetString("CaptchaCode", "ABCDE");

            var model = new RegisterViewModel { Email = "test@mail.com", Captcha = "ABCDE" };

            _accountServiceMock
                .Setup(s => s.ValidateRegistrationAsync(model, "ABCDE"))
                .ReturnsAsync(new Dictionary<string, string>());

            _accountServiceMock
                .Setup(s => s.CreateUserAsync(model))
                .ReturnsAsync(new Users { Email = "test@mail.com" });

            var result = await _controller.Register(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
            _accountServiceMock.Verify(s => s.CreateUserAsync(model), Times.Once);
        }

        [Fact]
        public void Login_Get_SetsCaptchaInSession_AndReturnsView()
        {
            _accountServiceMock.Setup(s => s.GenerateCaptchaCode()).Returns("ABCDE");


            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var result = _controller.Login();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("ABCDE", _controller.HttpContext.Session.GetString("CaptchaCode"));
            _accountServiceMock.Verify(s => s.GenerateCaptchaCode(), Times.Once);
        }
        [Fact]
        public void Login_Get_RedirectsToHome_WhenUserIsAuthenticated()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "testuser")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = principal,
                Session = new TestSession()
            };

            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "1"));

            var result = _controller.Login();

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Login_Post_ReturnsMessage_WhenModelStateIsInvalid()
        {
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                Session = new TestSession()
            };
            _controller.ModelState.AddModelError("Email", "Required");

            var model = new LoginViewModel();

            var result = await _controller.Login(model);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = JObject.FromObject(jsonResult.Value!);
            Assert.False((bool)data["success"]);
            Assert.Equal("Invalid input data.", (string)data["message"]);
        }
        [Fact]
        public async Task Login_Post_ReturnsView_WhenDataValidationFails()
        {
            var controller = new AccountController(_accountServiceMock.Object, _authServiceMock.Object);
            var session = new TestSession();
            session.SetString("CaptchaCode", "ABCDE");
            controller.ControllerContext.HttpContext = new DefaultHttpContext { Session = session };

            var model = new LoginViewModel { Email = "test@mail.com", Password = "123", Captcha = "wrong" };

            _accountServiceMock
                .Setup(s => s.ValidateLoginAsync(model, "ABCDE"))
                .ReturnsAsync((false, null, "Invalid CAPTCHA"));

            _accountServiceMock
                .Setup(s => s.GenerateCaptchaCode())
                .Returns("NEWCD");

            var result = await controller.Login(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.Equal("NEWCD", controller.HttpContext.Session.GetString("CaptchaCode"));
            Assert.True(controller.ModelState.ContainsKey("Captcha"));
        }
        [Fact]
        public async Task Login_Post_RedirectsToHome_WhenLoginSucceeds()
        {
            var controller = new AccountController(_accountServiceMock.Object, _authServiceMock.Object);
            var session = new TestSession();
            session.SetString("CaptchaCode", "ABCDE");
            controller.ControllerContext.HttpContext = new DefaultHttpContext { Session = session };

            var user = new Users { Email = "test@mail.com", Username = "tester" };
            var model = new LoginViewModel { Email = "test@mail.com", Password = "123", Captcha = "ABCDE", RememberMe = true };

            _accountServiceMock
                .Setup(s => s.ValidateLoginAsync(model, "ABCDE"))
                .ReturnsAsync((true, user, null));

            var result = await controller.Login(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
            _authServiceMock.Verify(a => a.SignInAsync(user, true), Times.Once);
        }
        [Fact]
        public async Task Logout_Post_CallsSignOutAndRedirectsToHome()
        {
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                Session = new TestSession()
            };

            var result = await _controller.Logout();

            _authServiceMock.Verify(a => a.SignOutAsync(), Times.Once);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }
        [Fact]
        public async Task Edit_Get_ReturnsView_WhenUserExists()
        {
            var userId = Guid.NewGuid();
            var user = new Users { Id = userId, Username = "tester", Email = "test@mail.com" };
            var model = new EditProfileViewModel { FirstName = "Test", LastName = "User" };

            _accountServiceMock.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _accountServiceMock.Setup(s => s.MapToEditProfileViewModel(user)).Returns(model);

            var claims = new List<Claim> { new Claim("UserId", userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = claimsPrincipal };

            var result = await _controller.Edit();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }
        [Fact]
        public async Task Edit_Get_ReturnsNotFound_WhenUserDoesNotExist()
        {
            var userId = Guid.NewGuid();
            _accountServiceMock.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync((Users?)null);

            var claims = new List<Claim> { new Claim("UserId", userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = claimsPrincipal };

            var result = await _controller.Edit();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ReturnsView_WhenModelStateIsInvalid()
        {
            var model = new EditProfileViewModel { FirstName = "", LastName = "" };

            var claims = new List<Claim> { new Claim("UserId", Guid.NewGuid().ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = claimsPrincipal };
            _controller.ModelState.AddModelError("FirstName", "Required");

            var result = await _controller.Edit(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);

        }
        [Fact]
        public async Task Edit_Post_UpdatesUserProfileAndRedirects_WhenModelStateIsValid()
        {
            var userId = Guid.NewGuid();
            var model = new EditProfileViewModel { FirstName = "Test", LastName = "User" };

            var claims = new List<Claim> { new Claim("UserId", userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var result = await _controller.Edit(model);

            _accountServiceMock.Verify(s => s.UpdateUserProfile(userId, model), Times.Once);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
            Assert.Equal("Profile updated successfully!", _controller.TempData["SuccessMessage"]);
        }
        [Fact]
        public async Task ChangePassword_Get_ReturnsView_WhenUserExists()
        {
            var userId = Guid.NewGuid();
            var user = new Users { Id = userId, Username = "tester", Email = "test@mail.com" };

            _accountServiceMock.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _accountServiceMock.Setup(s => s.GenerateCaptchaCode()).Returns("ABCDE");

            var claims = new List<Claim> { new Claim("UserId", userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = claimsPrincipal, Session = new TestSession() };
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = await _controller.ChangePassword();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ChangePasswordViewModel>(viewResult.Model);
            Assert.Equal(userId, model.Id);
            Assert.Equal("ABCDE", _controller.HttpContext.Session.GetString("CaptchaCode"));
        }

        [Fact]
        public async Task ChangePassword_Get_ReturnsNotFound_WhenUserDoesNotExist()
        {
            var userId = Guid.NewGuid();
            _accountServiceMock.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync((Users?)null);
            _accountServiceMock.Setup(s => s.GenerateCaptchaCode()).Returns("ABCDE");

            var claims = new List<Claim> { new Claim("UserId", userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = claimsPrincipal, Session = new TestSession() };
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = await _controller.ChangePassword();

            Assert.IsType<NotFoundResult>(result);
        }
        [Fact]
        public async Task ChangePassword_Post_ReturnsMessage_WhenModelStateIsInvalid()
        {
            var model = new ChangePasswordViewModel();
            var userId = Guid.NewGuid();

            var claims = new List<Claim> { new Claim("UserId", userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = principal, Session = new TestSession() };
            _controller.ModelState.AddModelError("OldPassword", "Required");

            var result = await _controller.ChangePassword(model);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = JObject.FromObject(jsonResult.Value!);
            Assert.False((bool)data["success"]);
            Assert.Equal("Model state is not valid", (string)data["message"]);
        }

        [Fact]
        public async Task ChangePassword_Post_ReturnsView_WhenChangePasswordFails()
        {
            var userId = Guid.NewGuid();
            var model = new ChangePasswordViewModel { Id = userId };
            var session = new TestSession();
            session.SetString("CaptchaCode", "ABCDE");

            var claims = new List<Claim> { new Claim("UserId", userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = principal, Session = session };
            _controller.TempData = new TempDataDictionary(_controller.HttpContext, Mock.Of<ITempDataProvider>());

            _accountServiceMock
                .Setup(s => s.ChangePasswordAsync(model, "ABCDE"))
                .ReturnsAsync((false, "Incorrect old password"));

            _accountServiceMock
                .Setup(s => s.GenerateCaptchaCode())
                .Returns("NEWCD");

            var result = await _controller.ChangePassword(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.Equal("NEWCD", _controller.HttpContext.Session.GetString("CaptchaCode"));
            Assert.True(_controller.ModelState.ContainsKey("OldPassword"));
        }
        [Fact]
        public async Task ChangePassword_Post_RedirectsToHome_WhenSuccessful()
        {
            var userId = Guid.NewGuid();
            var model = new ChangePasswordViewModel { Id = userId };

            var claims = new List<Claim> { new Claim("UserId", userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = principal, Session = new TestSession() };
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            _accountServiceMock
                .Setup(s => s.ChangePasswordAsync(model, It.IsAny<string>()))
                .ReturnsAsync((true, null));

            var result = await _controller.ChangePassword(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
            Assert.Equal("Password changed successfully!", _controller.TempData["SuccessMessage"]);
        }
        [Fact]
        public void CaptchaImage_ReturnsFile_WhenCaptchaExistsInSession()
        {
            var captchaCode = "ABCDE";
            var imageBytes = new byte[] { 1, 2, 3 };
            _accountServiceMock.Setup(s => s.GenerateCaptchaImage(captchaCode)).Returns(imageBytes);

            var session = new TestSession();
            session.SetString("CaptchaCode", captchaCode);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { Session = session };

            var result = _controller.CaptchaImage();

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("image/png", fileResult.ContentType);
            Assert.Equal(imageBytes, fileResult.FileContents);
            _accountServiceMock.Verify(s => s.GenerateCaptchaImage(captchaCode), Times.Once);
        }

        [Fact]
        public void CaptchaImage_ReturnsFileWithError_WhenCaptchaMissing()
        {
            var imageBytes = new byte[] { 9, 8, 7 };
            _accountServiceMock.Setup(s => s.GenerateCaptchaImage("ERROR")).Returns(imageBytes);

            var session = new TestSession();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { Session = session };


            var result = _controller.CaptchaImage();

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("image/png", fileResult.ContentType);
            Assert.Equal(imageBytes, fileResult.FileContents);
            _accountServiceMock.Verify(s => s.GenerateCaptchaImage("ERROR"), Times.Once);
        }


    }
}
