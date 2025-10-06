using System.Net;

namespace RegisterForm.Utilities
{
    public class StaticFileHandler
    {
        public static bool TryServe(HttpListenerRequest request, HttpListenerResponse response)
        {
            string path = request.Url.AbsolutePath;
            string localPath = Path.Combine("wwwroot", path.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (!File.Exists(localPath)) return false;

            byte[] buffer = File.ReadAllBytes(localPath);
            response.ContentType = GetContentType(localPath);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            return true;
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
                _ => "application/octet-stream"
            };
        }
    }
}
