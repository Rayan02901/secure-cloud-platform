using api_gateway_dotnet.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api_gateway_dotnet.Controllers
{
    [ApiController]
    [Route("crypto")]
    [Authorize]
    public class CryptoProxyController : ControllerBase
    {
        private readonly IProxyService _proxy;
        private readonly IConfiguration _config;

        public CryptoProxyController(
            IProxyService proxy,
            IConfiguration config
        )
        {
            _proxy = proxy;
            _config = config;
        }

        [HttpPost("encrypt")]
        public async Task<IActionResult> Encrypt()
        {
            var token = Request.Headers["Authorization"]
                .ToString().Replace("Bearer ", "");

            var url = $"{_config["http://localhost:8002"]}/encrypt";
            var response = await _proxy.ForwardAsync(url, Request, token);
            var content = await response.Content.ReadAsStringAsync();

            return StatusCode((int)response.StatusCode, content);
        }
    }
}
