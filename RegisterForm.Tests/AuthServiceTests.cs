using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RegisterForm.Data;
using RegisterForm.Repositories.IRepositories;
using RegisterForm.Services;

namespace RegisterForm.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            var context = new DefaultHttpContext();
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);
            _authService = new AuthService(_httpContextAccessorMock.Object);
        }

        [Fact]
        public async Task SignInAsync_CallsHttpContextSignInWithCorrectParameters()
        {
            var user = new Users
            {
                Id = Guid.NewGuid(),
                Email = "test@mail.com",
                Username = "tester"
            };

            var context = new DefaultHttpContext();
            var authServiceMock = new Mock<IAuthenticationService>();
            context.RequestServices = new ServiceCollection()
                .AddSingleton(authServiceMock.Object)
                .BuildServiceProvider();

            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);

            var authService = new AuthService(httpContextAccessorMock.Object);

            await authService.SignInAsync(user);
            authServiceMock.Verify(a => a.SignInAsync(
                context,
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<ClaimsPrincipal>(),
                It.Is<AuthenticationProperties>(p => p.IsPersistent && p.ExpiresUtc != null)),
                Times.Once);
        }
        [Fact]
        public async Task SignOutAsync_CallsHttpContextSignOutWithCorrectScheme()
        {
            var context = new DefaultHttpContext();

            var authServiceMock = new Mock<IAuthenticationService>();
            context.RequestServices = new ServiceCollection()
                .AddSingleton(authServiceMock.Object)
                .BuildServiceProvider();

            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);

            var authService = new AuthService(httpContextAccessorMock.Object);

            await authService.SignOutAsync();

            authServiceMock.Verify(a => a.SignOutAsync(
                context,
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<AuthenticationProperties>()),
                Times.Once);
        }
        [Fact]
        public void CreatePrincipal_ReturnsPrincipalWithCorrectClaims()
        {
            var user = new Users
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@mail.com"
            };

            var service = new AuthService(new Mock<IHttpContextAccessor>().Object);

            var principal = service.CreatePrincipal(user);

            Assert.NotNull(principal);
            var identity = Assert.Single(principal.Identities);
            Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, identity.AuthenticationType);

            Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.Name && c.Value == user.Username);
            Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.Email && c.Value == user.Email);
            Assert.Contains(principal.Claims, c => c.Type == "UserId" && c.Value == user.Id.ToString());
        }

    }
}
