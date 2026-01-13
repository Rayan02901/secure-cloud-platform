using api_gateway_dotnet.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace api_gateway_dotnet.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthProxyController : ControllerBase
    {
        private readonly IProxyService _proxy;
	private readonly IAuthCookieService _authCookie;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthProxyController> _logger;

        public AuthProxyController(
            IProxyService proxy,
	    IAuthCookieService authCookie,
            IConfiguration config,
            ILogger<AuthProxyController> logger
        )
        {
            _proxy = proxy;
	    _authCookie = authCookie;
            _config = config;
            _logger = logger;
        }

        [HttpPost("login")]
	[AllowAnonymous]
	public async Task<IActionResult> Login()
	{
	    var url = $"{_config["Services:Auth"]}/login";
	    var response = await _proxy.ForwardAsync(url, Request, token: null);
	    var content = await response.Content.ReadAsStringAsync();
	
	    if (!response.IsSuccessStatusCode)
	        return StatusCode((int)response.StatusCode, content);
	
	    var json = JsonDocument.Parse(content);
	    var token = json.RootElement.GetProperty("access_token").GetString();
	
	    _authCookie.SetJwtCookie(Response, token);
	
	    return Ok(new { message = "Login successful" });
	}


        [Authorize]
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyToken()
        {
            try
            {
                _logger.LogInformation("Processing token verification request");
                var token = ExtractBearerToken();
                
                var authServiceUrl = _config["Services:Auth"];
                var url = $"{authServiceUrl}/api/auth/verify";
                
                var response = await _proxy.ForwardAsync(url, Request, token);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Verify response status: {response.StatusCode}");
                return StatusCode((int)response.StatusCode, content);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Token verification failed - no valid token provided");
                return Unauthorized(new { error = "No valid authorization token provided" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying token");
                return StatusCode(500, new { error = "Internal server error during token verification" });
            }
        }

        [AllowAnonymous]
        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            try
            {
                _logger.LogDebug("Checking auth service health");
                var authServiceUrl = _config["Services:Auth"];
                var url = $"{authServiceUrl}/api/auth/health";
                
                var client = new HttpClient();
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Auth health check status: {response.StatusCode}");
                return StatusCode((int)response.StatusCode, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking auth service health");
                return StatusCode(503, new 
                { 
                    status = "unhealthy",
                    service = "auth-service",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [Authorize]
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                _logger.LogInformation("Processing token refresh request");
                var token = ExtractBearerToken();
                
                var authServiceUrl = _config["Services:Auth"];
                var url = $"{authServiceUrl}/api/auth/refresh";
                
                var response = await _proxy.ForwardAsync(url, Request, token);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Refresh response status: {response.StatusCode}");
                return StatusCode((int)response.StatusCode, content);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Token refresh failed - no valid token provided");
                return Unauthorized(new { error = "No valid authorization token provided" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new { error = "Internal server error during token refresh" });
            }
        }

        [Authorize]
        [HttpGet("userinfo")]
        public async Task<IActionResult> GetUserInfo()
        {
            try
            {
                _logger.LogInformation("Processing user info request");
                var token = ExtractBearerToken();
                
                var authServiceUrl = _config["Services:Auth"];
                var url = $"{authServiceUrl}/api/auth/userinfo";
                
                var response = await _proxy.ForwardAsync(url, Request, token);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"User info response status: {response.StatusCode}");
                return StatusCode((int)response.StatusCode, content);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("User info request failed - no valid token provided");
                return Unauthorized(new { error = "No valid authorization token provided" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info");
                return StatusCode(500, new { error = "Internal server error fetching user information" });
            }
        }

	[Authorize]
        [HttpPost("logout")]
	public IActionResult Logout()
	{
	    _authCookie.ClearJwtCookie(Response);
	    return Ok(new { message = "Logged out" });
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