using System;
using System.IO;
using System.Text;
using Xunit;
using RegisterForm.Utilities;

namespace RegisterForm.Tests
{
    public class StaticFileHandlerTests
    {
        [Fact]
        public void TryReadFile_FileExists_ReturnsContentAndType()
        {
            Directory.CreateDirectory("wwwroot");
            File.WriteAllText("wwwroot/test.txt", "Hello, world!");

            var result = StaticFileHandler.TryReadFile("/test.txt");

            Assert.NotNull(result);
            Assert.Equal("application/octet-stream", result.Value.ContentType);
            Assert.Equal("Hello, world!", Encoding.UTF8.GetString(result.Value.Content));

            File.Delete("wwwroot/test.txt");
        }

        [Fact]
        public void TryReadFile_FileDoesNotExist_ReturnsNull()
        {
            var result = StaticFileHandler.TryReadFile("/nonexistent.txt");
            Assert.Null(result);
        }

        [Theory]
        [InlineData(".css", "text/css")]
        [InlineData(".js", "application/javascript")]
        [InlineData(".png", "image/png")]
        [InlineData(".jpg", "image/jpeg")]
        [InlineData(".jpeg", "application/octet-stream")]
        public void TryReadFile_ContentTypes_AreCorrect(string extension, string expected)
        {
            Directory.CreateDirectory("wwwroot");
            string fileName = "file" + extension;
            File.WriteAllText(Path.Combine("wwwroot", fileName), "data");

            var result = StaticFileHandler.TryReadFile("/" + fileName);

            Assert.NotNull(result);
            Assert.Equal(expected, result.Value.ContentType);
            Assert.Equal("data", Encoding.UTF8.GetString(result.Value.Content));

            File.Delete(Path.Combine("wwwroot", fileName));
        }
    }
}
public static class StaticFileHandler
{
    public static (byte[] Content, string ContentType)? TryReadFile(string urlPath)
    {
        string localPath = Path.Combine("wwwroot", urlPath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (!File.Exists(localPath)) return null;

        byte[] content = File.ReadAllBytes(localPath);
        string contentType = GetContentType(localPath);

        return (content, contentType);
    }

    private static string GetContentType(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        return ext switch
        {
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".html" => "text/html",
            _ => "application/octet-stream"
        };
    }
}
