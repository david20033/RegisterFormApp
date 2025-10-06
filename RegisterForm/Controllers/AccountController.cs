
using RegisterForm.Services.IServices;
using RegisterForm.ViewModels;


namespace RegisterForm.Controllers
{
    public class AccountController : BaseController
    {
        private readonly IAccountService _accountService;
        public bool IsAuthenticated;
        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
            IsAuthenticated = CurrentUserId != null;
        }
        public string RegisterGet()
        {
            if (IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            string captcha = _accountService.GenerateCaptchaCode();
            Session["CaptchaCode"] = captcha;
            return View("Register.html", new RegisterViewModel { });
        }
        public async Task<string> RegisterPost(RegisterViewModel model)
        {
            if (IsAuthenticated)
                return RedirectToAction( "Index", "Home");

            var sessionCaptcha = Session.GetValueOrDefault("CaptchaCode") ?? "";
            var errors = await _accountService.ValidateRegistrationAsync(model, sessionCaptcha);

            if (errors.Count > 0)
            {
                string captchaCode = _accountService.GenerateCaptchaCode();
                Session["CaptchaCode"] = captchaCode;

                return View("Register.html", new
                {
                    Model = model,
                    Errors = errors,
                    Captcha = captchaCode
                });
            }

            await _accountService.CreateUserAsync(model);
            Session["CaptchaCode"] = _accountService.GenerateCaptchaCode(); ;

            return RedirectToAction("Login", "Account");
        }
        public string LoginGet()
        {
            if (IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            string captchaCode = _accountService.GenerateCaptchaCode();
            Session["CaptchaCode"] = captchaCode;

            return View("Login.html", new
            {
                Captcha = captchaCode
            });
        }

        public async Task<string> LoginPost(LoginViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                return Json(new { success = false, message = "Invalid input data." });
            }

            string sessionCaptcha = Session.GetValueOrDefault("CaptchaCode") ?? "";

            var (isValid, user, errorMessage) = await _accountService.ValidateLoginAsync(model, sessionCaptcha);

            if (!isValid)
            {
                string captchaCode = _accountService.GenerateCaptchaCode();
                Session["CaptchaCode"] = captchaCode;

                return View("Login.html", new
                {
                    Model = model,
                    ErrorMessage = errorMessage,
                    Captcha = captchaCode
                });
            }

            CurrentUserId = user.Id;
            CurrentUserUsername = user.Username;
            IsAuthenticated = true;

            return RedirectToAction("Index", "Home");
        }

        public string Logout()
        {
            CurrentUserId = null;
            CurrentUserUsername = string.Empty;
            IsAuthenticated = false;
            return RedirectToAction("Index", "Home");
        }
        public async Task<string> EditGet()
        {
            if (!IsAuthenticated)
                return RedirectToAction("Login", "Account");

            var user = await _accountService.GetUserByIdAsync(CurrentUserId.Value);
            if (user == null)
            {
                return "<h1>User not found</h1>";
            }

            var model = _accountService.MapToEditProfileViewModel(user);

            return View("Edit.html", model);
        }
        public async Task<string> EditPost(EditProfileViewModel model)
        {
            if (!IsAuthenticated)
                return RedirectToAction("Login", "Account");


            await _accountService.UpdateUserProfile(CurrentUserId.Value, model);

            Session["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Index", "Home");
        }

        public async Task<string> ChangePasswordGet()
        {
            if (!IsAuthenticated)
                return RedirectToAction("Login", "Account");

            string captchaCode = _accountService.GenerateCaptchaCode();
            Session["CaptchaCode"] = captchaCode;

            var user = await _accountService.GetUserByIdAsync(CurrentUserId.Value);
            if (user == null)
            {
                return "<h1>User not found</h1>";
            }

            var model = new ChangePasswordViewModel
            {
                Id = CurrentUserId.Value
            };

            return View("ChangePassword.html", model);
        }
        public async Task<string> ChangePasswordPost(ChangePasswordViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.OldPassword) || string.IsNullOrWhiteSpace(model.NewPassword))
                return Json(new { success = false, message = "Model state is not valid" });

            var userId = CurrentUserId.Value;
            var sessionCaptcha = Session.GetValueOrDefault("CaptchaCode") ?? "";
            model.Id = CurrentUserId??Guid.Empty;
            var (success, errorMessage) = await _accountService.ChangePasswordAsync(model, sessionCaptcha);

            if (!success)
            {
                string captchaCode = _accountService.GenerateCaptchaCode();
                Session["CaptchaCode"] = captchaCode;

                return View("ChangePassword.html", new
                {
                    Model = model,
                    ErrorMessage = errorMessage,
                    Captcha = captchaCode
                });
            }

            Session["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction("Index", "Home");
        }
        public byte[] CaptchaImage()
        {
            string captchaCode = Session.GetValueOrDefault("CaptchaCode") ?? "ERROR";
            byte[] image = _accountService.GenerateCaptchaImage(captchaCode);
            return image;
        }


    }
}
