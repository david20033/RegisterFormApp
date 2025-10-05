
using System.Drawing.Imaging;
using System.Drawing;
using Azure.Messaging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RegisterForm.Data;
using RegisterForm.Services.IServices;
using RegisterForm.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace RegisterForm.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IAuthService _authService;
        public AccountController(IAccountService accountService, IAuthService authService)
        {
            _accountService = accountService;
            _authService = authService;
        }
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            var captchaCode = _accountService.GenerateCaptchaCode();
            HttpContext.Session.SetString("CaptchaCode", captchaCode);
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var sessionCaptcha = HttpContext.Session.GetString("CaptchaCode");
            var errors = await _accountService.ValidateRegistrationAsync(model, sessionCaptcha);

            if (errors.Any())
            {
                foreach (var error in errors)
                    ModelState.AddModelError(error.Key, error.Value);

                var captchaCode = _accountService.GenerateCaptchaCode();
                HttpContext.Session.SetString("CaptchaCode", captchaCode);

                return View(model);
            }

            await _accountService.CreateUserAsync(model);
            return RedirectToAction("Login", "Account");
        }
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            var captchaCode = _accountService.GenerateCaptchaCode();
            HttpContext.Session.SetString("CaptchaCode", captchaCode);
            return View();
        }
            [HttpPost]
            [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid input data." });

            string sessionCaptcha = HttpContext.Session.GetString("CaptchaCode");
            var (isValid, user, errorMessage) = await _accountService.ValidateLoginAsync(model, sessionCaptcha);

            if (!isValid)
            {
                var captchaCode = _accountService.GenerateCaptchaCode();
                HttpContext.Session.SetString("CaptchaCode", captchaCode);

                ModelState.AddModelError(string.Empty ,errorMessage);
                return View(model);
            }

            await _authService.SignInAsync(user!, model.RememberMe);
            return RedirectToAction("Index", "Home");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _authService.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
        [Authorize]
        public async Task<IActionResult> Edit()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var user = await _accountService.GetUserByIdAsync(Guid.Parse(userId));
            if(user == null)
            {
                return NotFound();
            }
            var model = _accountService.MapToEditProfileViewModel(user);
            return View(model);
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirst("UserId")?.Value;
                await _accountService.UpdateUserProfile(Guid.Parse(userId), model);
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }
        [Authorize]
        public async Task<IActionResult> ChangePassword()
        {
            var captchaCode = _accountService.GenerateCaptchaCode();
            HttpContext.Session.SetString("CaptchaCode", captchaCode);
            var userId = User.FindFirst("UserId")?.Value;
            var user = await _accountService.GetUserByIdAsync(Guid.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }
            return View(new ChangePasswordViewModel { Id=Guid.Parse(userId)});
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Model state is not valid" });

            var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
            var sessionCaptcha = HttpContext.Session.GetString("CaptchaCode");

            var (success, errorMessage) = await _accountService.ChangePasswordAsync(model, sessionCaptcha);

            if (!success)
            {
                var captchaCode = _accountService.GenerateCaptchaCode();
                HttpContext.Session.SetString("CaptchaCode", captchaCode);

                ModelState.AddModelError(string.IsNullOrEmpty(errorMessage) ? "Error" : "OldPassword", errorMessage!);
                return View(model);
            }

            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction("Index", "Home");
        }
        public IActionResult CaptchaImage()
        {
            var captchaCode = HttpContext.Session.GetString("CaptchaCode") ?? "ERROR";
            var image = _accountService.GenerateCaptchaImage(captchaCode);

            return File(image, "image/png");
        }


    }
}
