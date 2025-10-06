
namespace RegisterForm.Controllers
{
    public class HomeController : BaseController
    {
        public bool IsAuthenticated { get; set; }
        public string Username { get; set; }
        public string Index()
        {
            if (IsAuthenticated) 
            {
                return View("Index1.html", new
                {
                    Username = Username,    
                });
            }
            else
            {
                return View("Index.html");
            }
        }
    }
}
