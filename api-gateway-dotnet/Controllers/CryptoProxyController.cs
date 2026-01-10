using api_gateway_dotnet.Services.Interface;
using Microsoft.AspNetCore.Authorization;
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
        private readonly ILogger<CryptoProxyController> _logger;

        public CryptoProxyController(
            IProxyService proxy,
            IConfiguration config,
            ILogger<CryptoProxyController> logger
        )
        {
            _proxy = proxy;
            _config = config;
            _logger = logger;
        }

        [HttpPost("encrypt")]
        public async Task<IActionResult> Encrypt()
        {
            try
            {
                _logger.LogInformation("Processing encrypt request");
                var token = ExtractBearerToken();
                
                var cryptoServiceUrl = _config["Services:Crypto"];
                _logger.LogDebug($"Crypto service URL: {cryptoServiceUrl}");
                
                var url = $"{cryptoServiceUrl}/encrypt";
                var response = await _proxy.ForwardAsync(url, Request, token);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Encrypt response status: {response.StatusCode}");
                return StatusCode((int)response.StatusCode, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing encrypt request");
                return StatusCode(500, new { error = "Internal server error during encryption" });
            }
        }

        [HttpPost("decrypt")]
        public async Task<IActionResult> Decrypt()
        {
            try
            {
                _logger.LogInformation("Processing decrypt request");
                var token = ExtractBearerToken();
                
                var cryptoServiceUrl = _config["Services:Crypto"];
                var url = $"{cryptoServiceUrl}/decrypt";
                var response = await _proxy.ForwardAsync(url, Request, token);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Decrypt response status: {response.StatusCode}");
                return StatusCode((int)response.StatusCode, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing decrypt request");
                return StatusCode(500, new { error = "Internal server error during decryption" });
            }
        }

        [AllowAnonymous]
        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            try
            {
                _logger.LogDebug("Processing health check request");
                var cryptoServiceUrl = _config["Services:Crypto"];
                var url = $"{cryptoServiceUrl}/health";
                
                var client = new HttpClient();
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Health check status: {response.StatusCode}");
                return StatusCode((int)response.StatusCode, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking crypto service health");
                return StatusCode(503, new 
                { 
                    status = "unhealthy",
                    service = "crypto-service",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        private string ExtractBearerToken()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("No Bearer token found in request");
                throw new UnauthorizedAccessException("No valid authorization token provided");
            }

            return authHeader.Replace("Bearer ", "");
        }
    }
}