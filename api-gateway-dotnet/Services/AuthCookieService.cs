using Microsoft.AspNetCore.Http;
using api_gateway_dotnet.Services.Interface;
namespace api_gateway_dotnet.Services
{
    public class AuthCookieService : IAuthCookieService
    {
        public void SetJwtCookie(
            HttpResponse response,
            string token
        )
        {
            response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // true in prod (HTTPS)
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });
        }

        public void ClearJwtCookie(HttpResponse response)
        {
            response.Cookies.Delete("access_token");
        }
    }
}
