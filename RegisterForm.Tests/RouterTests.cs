using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Moq;

namespace RegisterForm.Tests
{
    public interface IHttpContext
    {
        string Method { get; }
        Uri Url { get; }
        Stream InputStream { get; }
        Encoding ContentEncoding { get; }
        Stream OutputStream { get; }
        int StatusCode { get; set; }
        string ContentType { get; set; }
        string RedirectLocation { get; set; }
    }

    public class Router
    {
        private readonly Dictionary<string, string> _session = new Dictionary<string, string>();

        public async Task RouteAsync(IHttpContext context)
        {
            var request = context;
            var response = context;
            string path = request.Url.AbsolutePath.ToLower();
            string responseString = "";

            try
            {
                string localPath = Path.Combine("wwwroot", path.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (File.Exists(localPath))
                {
                    byte[] buffer = File.ReadAllBytes(localPath);
                    response.ContentType = GetContentType(localPath);
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                    return;
                }

                if (path == "/account/captchaimage")
                {
                    byte[] imageBytes = Encoding.UTF8.GetBytes("FAKE_IMAGE"); 
                    response.ContentType = "image/png";
                    await response.OutputStream.WriteAsync(imageBytes, 0, imageBytes.Length);
                    response.OutputStream.Close();
                    return;
                }

                if (path.StartsWith("/account/register"))
                {
                    if (request.Method == "GET")
                    {
                        _session["CaptchaCode"] = "123"; 
                        responseString = "<html>Register View</html>";
                    }
                    else if (request.Method == "POST")
                    {
                        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                        string body = await reader.ReadToEndAsync();
                        responseString = "<html>Register POST View</html>";
                    }
                }
                else if (path.StartsWith("/account/login"))
                {
                    responseString = "<html>Login View</html>";
                }
                else if (path.StartsWith("/account/logout"))
                {
                    if (request.Method == "POST")
                    {
                        response.StatusCode = 302;
                        response.RedirectLocation = "/Home/Index";
                        response.OutputStream.Close();
                        return;
                    }
                }
                else
                {
                    responseString = "<h1>404 Not Found</h1>";
                }

                byte[] bufferOut = Encoding.UTF8.GetBytes(responseString);
                response.ContentType = "text/html";
                await response.OutputStream.WriteAsync(bufferOut, 0, bufferOut.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                string error = $"<h1>500 Internal Server Error</h1><pre>{ex}</pre>";
                byte[] bufferErr = Encoding.UTF8.GetBytes(error);
                response.ContentType = "text/html";
                await response.OutputStream.WriteAsync(bufferErr, 0, bufferErr.Length);
                response.OutputStream.Close();
            }
        }

        private string GetContentType(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext switch
            {
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".html" => "text/html",
                _ => "application/octet-stream"
            };
        }
    }

    public class RouterTests
    {
        private Router GetRouter() => new Router();

        private IHttpContext GetMockContext(string url, string method = "GET", string body = null)
        {
            var mock = new Mock<IHttpContext>();
            mock.Setup(c => c.Method).Returns(method);
            mock.Setup(c => c.Url).Returns(new Uri("http://localhost" + url));

            var msInput = string.IsNullOrEmpty(body) ? new NonClosingMemoryStream() : new MemoryStream(Encoding.UTF8.GetBytes(body));
            mock.Setup(c => c.InputStream).Returns(msInput);
            mock.Setup(c => c.ContentEncoding).Returns(Encoding.UTF8);

            var msOutput = new NonClosingMemoryStream();
            mock.Setup(c => c.OutputStream).Returns(msOutput);
            mock.SetupProperty(c => c.StatusCode);
            mock.SetupProperty(c => c.ContentType);
            mock.SetupProperty(c => c.RedirectLocation);

            return mock.Object;
        }

        [Fact]
        public async Task RouteAsync_ServesStaticFile()
        {
            var router = GetRouter();
            Directory.CreateDirectory("wwwroot");
            string testFile = Path.Combine("wwwroot", "test.html");
            await File.WriteAllTextAsync(testFile, "<h1>Test</h1>");

            var context = GetMockContext("/test.html");
            await router.RouteAsync(context);

            Assert.Equal("text/html", context.ContentType);
            Assert.True(context.OutputStream.Length > 0);

            File.Delete(testFile);
        }

        [Fact]
        public async Task RouteAsync_CaptchaImage_ReturnsImage()
        {
            var router = GetRouter();
            var context = GetMockContext("/account/captchaimage");

            await router.RouteAsync(context);

            Assert.Equal("image/png", context.ContentType);
            Assert.True(context.OutputStream.Length > 0);
        }

        [Fact]
        public async Task RouteAsync_RegisterGet_ReturnsHtml()
        {
            var router = GetRouter();
            var context = GetMockContext("/account/register", "GET");

            await router.RouteAsync(context);

            Assert.Equal("text/html", context.ContentType);
            Assert.True(context.OutputStream.Length > 0);
        }

        [Fact]
        public async Task RouteAsync_LogoutPost_Redirects()
        {
            var router = GetRouter();
            var context = GetMockContext("/account/logout", "POST");

            await router.RouteAsync(context);

            Assert.Equal(302, context.StatusCode);
            Assert.Equal("/Home/Index", context.RedirectLocation);
        }

        [Fact]
        public async Task RouteAsync_NotFound_Returns404()
        {
            var router = GetRouter();
            var context = GetMockContext("/nonexistent", "GET");

            await router.RouteAsync(context);

            Assert.Equal("text/html", context.ContentType);
            context.OutputStream.Position = 0;
            var responseText = new StreamReader(context.OutputStream).ReadToEnd();
            Assert.Contains("404 Not Found", responseText);
        }
    }
}

public class NonClosingMemoryStream : MemoryStream
{
    public override void Close()
    {
    }
}
