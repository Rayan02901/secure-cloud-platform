using Microsoft.AspNetCore.Http;

namespace api_gateway_dotnet.Services.Interface
{
    public interface IAuthCookieService
    {
        void SetJwtCookie(HttpResponse response, string token);
	void ClearJwtCookie(HttpResponse response);
    }
}
