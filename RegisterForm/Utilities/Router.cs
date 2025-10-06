using RegisterForm.Controllers;
using RegisterForm.Repositories;
using RegisterForm.Services;
using RegisterForm.ViewModels;
using System.Net;
using System.Text;

namespace RegisterForm.Utilities
{
    public class Router
    {
        private readonly AccountController _accountController;
        private readonly HomeController _homeController;
        private readonly Dictionary<string, string> _session;

        public Router()
        {
            _session = new Dictionary<string, string>();
            _accountController = new AccountController(
                new AccountService(
                    new AccountRepository(
                        "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=RegisterForm;Integrated Security=True"
                    )
                )
            )
            {
                Session = _session
            };

            _homeController = new HomeController();
        }

        public async Task RouteAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            string path = request.Url.AbsolutePath.ToLower();
            string responseString = "";

            try
            {
                string localPath = Path.Combine("wwwroot", path.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (System.IO.File.Exists(localPath))
                {
                    byte[] buffer = System.IO.File.ReadAllBytes(localPath);
                    response.ContentType = GetContentType(localPath);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                    return;
                }

                if (path == "/account/captchaimage")
                {
                    byte[] imageBytes = _accountController.CaptchaImage();
                    response.ContentType = "image/png";
                    response.ContentLength64 = imageBytes.Length;
                    response.OutputStream.Write(imageBytes, 0, imageBytes.Length);
                    response.OutputStream.Close();
                    return;
                }

                if (path.StartsWith("/account/register"))
                {
                    if (request.HttpMethod == "GET")
                        responseString = _accountController.RegisterGet();
                    else if (request.HttpMethod == "POST")
                    {
                        var formData = await ReadFormData(request);
                        var model = new RegisterViewModel
                        {
                            FirstName = formData.GetValueOrDefault("FirstName"),
                            LastName = formData.GetValueOrDefault("LastName"),
                            Username = formData.GetValueOrDefault("Username"),
                            Email = formData.GetValueOrDefault("Email"),
                            Password = formData.GetValueOrDefault("Password"),
                            ConfirmPassword = formData.GetValueOrDefault("ConfirmPassword"),
                            PhoneNumber = formData.GetValueOrDefault("PhoneNumber"),
                            DateOfBirth = DateTime.TryParse(formData.GetValueOrDefault("DateOfBirth"), out var dob) ? dob : DateTime.MinValue,
                            Captcha = formData.GetValueOrDefault("Captcha")
                        };
                        responseString = _accountController.RegisterPost(model).Result;
                    }
                }
                else if (path.StartsWith("/account/login"))
                {
                    if (request.HttpMethod == "GET")
                        responseString = _accountController.LoginGet();
                    else if (request.HttpMethod == "POST")
                    {
                        var formData = await ReadFormData(request);
                        var model = new LoginViewModel
                        {
                            Email = formData.GetValueOrDefault("Email"),
                            Password = formData.GetValueOrDefault("Password"),
                            RememberMe = formData.GetValueOrDefault("RememberMe") == "on",
                            Captcha = formData.GetValueOrDefault("Captcha")
                        };

                        responseString = _accountController.LoginPost(model).Result;

                        if (_accountController.CurrentUserId != null)
                        {
                            _homeController.IsAuthenticated = true;
                            _homeController.Username = model.Email;
                        }
                    }
                }
                else if (path.StartsWith("/account/logout"))
                {
                    if (request.HttpMethod == "POST")
                    {
                        _homeController.IsAuthenticated = false;
                        _homeController.Username = null;
                        _accountController.CurrentUserId = null;
                        _accountController.IsAuthenticated = false;
                        _accountController.Session.Clear();
                        responseString = "REDIRECT:/Home/Index";
                    }
                    else
                    {
                        responseString = "<h1>405 Method Not Allowed</h1>";
                    }
                }
                else if (path.StartsWith("/account/edit"))
                {
                    if (!_homeController.IsAuthenticated)
                    {
                        responseString = "REDIRECT:/Account/Login";
                    }
                    else if (request.HttpMethod == "GET")
                    {
                        responseString = _accountController.EditGet().Result;
                    }
                    else if (request.HttpMethod == "POST")
                    {
                        var formData = await ReadFormData(request);
                        var model = new EditProfileViewModel
                        {
                            FirstName = formData.GetValueOrDefault("FirstName"),
                            LastName = formData.GetValueOrDefault("LastName"),
                            Username = formData.GetValueOrDefault("Username"),
                            Email = formData.GetValueOrDefault("Email"),
                            PhoneNumber = formData.GetValueOrDefault("PhoneNumber"),
                            DateOfBirth = DateTime.TryParse(formData.GetValueOrDefault("DateOfBirth"), out var dob) ? dob : DateTime.MinValue
                        };
                        responseString = _accountController.EditPost(model).Result;
                    }
                }
                else if (path.StartsWith("/account/changepassword"))
                {
                    if (!_homeController.IsAuthenticated)
                    {
                        responseString = "REDIRECT:/Account/Login";
                    }
                    else if (request.HttpMethod == "GET")
                    {
                        responseString = _accountController.ChangePasswordGet().Result;
                    }
                    else if (request.HttpMethod == "POST")
                    {
                        var formData = await ReadFormData(request);
                        var model = new ChangePasswordViewModel
                        {
                            OldPassword = formData.GetValueOrDefault("OldPassword"),
                            NewPassword = formData.GetValueOrDefault("NewPassword"),
                            ConfirmPassword = formData.GetValueOrDefault("ConfirmPassword"),
                            Captcha = formData.GetValueOrDefault("Captcha")
                        };
                        responseString = _accountController.ChangePasswordPost(model).Result;
                    }
                }
                else if (path.StartsWith("/home/index") || path == "/")
                {
                    responseString = _homeController.Index();
                }
                else
                {
                    responseString = "<h1>404 Not Found</h1>";
                }

                if (!string.IsNullOrEmpty(responseString) && responseString.StartsWith("REDIRECT:"))
                {
                    string targetUrl = responseString.Substring("REDIRECT:".Length);
                    response.StatusCode = 302;
                    response.RedirectLocation = targetUrl;
                    response.ContentLength64 = 0;
                    response.OutputStream.Close();
                    return;
                }

                byte[] bufferOut = Encoding.UTF8.GetBytes(responseString);
                response.ContentType = "text/html";
                response.ContentLength64 = bufferOut.Length;
                response.OutputStream.Write(bufferOut, 0, bufferOut.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                string error = $"<h1>500 Internal Server Error</h1><pre>{ex}</pre>";
                byte[] bufferErr = Encoding.UTF8.GetBytes(error);
                response.ContentType = "text/html";
                response.ContentLength64 = bufferErr.Length;
                response.OutputStream.Write(bufferErr, 0, bufferErr.Length);
                response.OutputStream.Close();
            }
        }

        private static string GetContentType(string path)
        {
            string ext = System.IO.Path.GetExtension(path).ToLower();
            return ext switch
            {
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".html" => "text/html",
                _ => "application/octet-stream",
            };
        }

        private static async Task<Dictionary<string, string>> ReadFormData(HttpListenerRequest request)
        {
            using var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding);
            string body = await reader.ReadToEndAsync();
            var dict = new Dictionary<string, string>();
            foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = pair.Split('=', 2);
                if (kv.Length == 2)
                    dict[WebUtility.UrlDecode(kv[0])] = WebUtility.UrlDecode(kv[1]);
            }
            return dict;
        }
    }
}
