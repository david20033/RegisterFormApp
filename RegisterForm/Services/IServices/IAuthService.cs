using RegisterForm.Data;
using System.Security.Claims;

namespace RegisterForm.Services.IServices
{
    public interface IAuthService
    {
        Task SignInAsync(Users user, bool isPersistent = true);
        Task SignOutAsync();
        ClaimsPrincipal CreatePrincipal(Users user);
    }
}
