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

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login()
        {
            // Add correlation ID for better tracing
            var correlationId = Guid.NewGuid().ToString();
            
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["Endpoint"] = "auth/login",
                ["Method"] = Request.Method
            }))
            {
                try
                {
                    _logger.LogInformation("Processing login request");
                    
                    var authServiceUrl = _config["Services:Auth"];
                    _logger.LogDebug("Auth service URL: {AuthServiceUrl}", authServiceUrl);
                    
                    var url = $"{authServiceUrl}/api/auth/login";
                    
                    _logger.LogInformation("Forwarding login request to {Url}", url);
                    var response = await _proxy.ForwardAsync(url, Request, null);
                    var content = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation(
                        "Login response received - Status: {StatusCode}, ContentLength: {ContentLength}",
                        response.StatusCode,
                        content.Length
                    );
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Login successful");
                        
                        // Extract token from response if login was successful
                        try
                        {
                            var result = JsonSerializer.Deserialize<JsonElement>(content);
                            if (result.TryGetProperty("token", out var tokenElement))
                            {
                                var token = tokenElement.GetString();
                                if (!string.IsNullOrEmpty(token))
                                {
                                    // Set JWT as HttpOnly cookie
                                    _authCookie.SetJwtCookie(Response, token);
                                    _logger.LogDebug("JWT cookie set successfully");
                                    
                                    // Log token info (without exposing the actual token)
                                    _logger.LogInformation(
                                        "Token received and stored in cookie. Token length: {TokenLength}",
                                        token.Length
                                    );
                                    
                                    // Return minimal success response
                                    return Ok(new 
                                    { 
                                        success = true, 
                                        message = "Authentication successful" 
                                    });
                                }
                                else
                                {
                                    _logger.LogWarning("Token property exists but is null or empty");
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Login response doesn't contain 'token' property. Response: {@Response}", 
                                    new { StatusCode = response.StatusCode, HasContent = !string.IsNullOrEmpty(content) });
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogError(jsonEx, "Failed to parse JSON response from auth service");
                        }
                        catch (Exception parseEx)
                        {
                            _logger.LogError(parseEx, "Failed to parse token from auth response");
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Login failed with status: {StatusCode}. Response: {ResponseContent}",
                            response.StatusCode,
                            content
                        );
                    }

                    // For non-success responses or if token extraction failed, return original
                    return StatusCode((int)response.StatusCode, content);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing login request. CorrelationId: {CorrelationId}", correlationId);
                    return StatusCode(500, new 
                    { 
                        error = "Internal server error during authentication",
                        correlationId = correlationId 
                    });
                }
            }
        }

        [Authorize]
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyToken()
        {
            var correlationId = Guid.NewGuid().ToString();
            
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["Endpoint"] = "auth/verify",
                ["Method"] = Request.Method,
                ["UserId"] = User.Identity?.Name ?? "unknown"
            }))
            {
                try
                {
                    _logger.LogInformation("Processing token verification request");
                    var token = ExtractBearerToken();
                    
                    var authServiceUrl = _config["Services:Auth"];
                    var url = $"{authServiceUrl}/api/auth/verify";
                    
                    _logger.LogDebug("Forwarding to auth service: {Url}", url);
                    var response = await _proxy.ForwardAsync(url, Request, token);
                    var content = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation(
                        "Token verification response - Status: {StatusCode}",
                        response.StatusCode
                    );
                    
                    return StatusCode((int)response.StatusCode, content);
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    _logger.LogWarning(uaEx, "Token verification failed - no valid token provided");
                    return Unauthorized(new 
                    { 
                        error = "No valid authorization token provided",
                        correlationId = correlationId 
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error verifying token. CorrelationId: {CorrelationId}", correlationId);
                    return StatusCode(500, new 
                    { 
                        error = "Internal server error during token verification",
                        correlationId = correlationId 
                    });
                }
            }
        }

        [AllowAnonymous]
        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            var correlationId = Guid.NewGuid().ToString();
            
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["Endpoint"] = "auth/health",
                ["Method"] = Request.Method
            }))
            {
                try
                {
                    _logger.LogInformation("Checking auth service health");
                    var authServiceUrl = _config["Services:Auth"];
                    
                    if (string.IsNullOrEmpty(authServiceUrl))
                    {
                        _logger.LogError("Auth service URL is not configured");
                        return StatusCode(503, new 
                        { 
                            status = "unhealthy",
                            service = "auth-service",
                            error = "Service URL not configured",
                            correlationId = correlationId,
                            timestamp = DateTime.UtcNow
                        });
                    }
                    
                    var url = $"{authServiceUrl}/api/auth/health";
                    _logger.LogDebug("Health check URL: {Url}", url);
                    
                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(5);
                    
                    var response = await client.GetAsync(url);
                    var content = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation(
                        "Auth health check completed - Status: {StatusCode}, Response: {Content}",
                        response.StatusCode,
                        content
                    );
                    
                    return StatusCode((int)response.StatusCode, content);
                }
                catch (TaskCanceledException timeoutEx)
                {
                    _logger.LogError(timeoutEx, "Auth service health check timed out. CorrelationId: {CorrelationId}", correlationId);
                    return StatusCode(503, new 
                    { 
                        status = "unhealthy",
                        service = "auth-service",
                        error = "Service timeout",
                        correlationId = correlationId,
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking auth service health. CorrelationId: {CorrelationId}", correlationId);
                    return StatusCode(503, new 
                    { 
                        status = "unhealthy",
                        service = "auth-service",
                        error = ex.Message,
                        correlationId = correlationId,
                        timestamp = DateTime.UtcNow
                    });
                }
            }
        }

        [Authorize]
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            var correlationId = Guid.NewGuid().ToString();
            
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["Endpoint"] = "auth/refresh",
                ["Method"] = Request.Method,
                ["UserId"] = User.Identity?.Name ?? "unknown"
            }))
            {
                try
                {
                    _logger.LogInformation("Processing token refresh request");
                    var token = ExtractBearerToken();
                    
                    var authServiceUrl = _config["Services:Auth"];
                    var url = $"{authServiceUrl}/api/auth/refresh";
                    
                    _logger.LogDebug("Forwarding refresh request to: {Url}", url);
                    var response = await _proxy.ForwardAsync(url, Request, token);
                    var content = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation(
                        "Token refresh response - Status: {StatusCode}",
                        response.StatusCode
                    );
                    
                    return StatusCode((int)response.StatusCode, content);
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    _logger.LogWarning(uaEx, "Token refresh failed - no valid token provided");
                    return Unauthorized(new 
                    { 
                        error = "No valid authorization token provided",
                        correlationId = correlationId 
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing token. CorrelationId: {CorrelationId}", correlationId);
                    return StatusCode(500, new 
                    { 
                        error = "Internal server error during token refresh",
                        correlationId = correlationId 
                    });
                }
            }
        }

        [Authorize]
        [HttpGet("userinfo")]
        public async Task<IActionResult> GetUserInfo()
        {
            var correlationId = Guid.NewGuid().ToString();
            
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["Endpoint"] = "auth/userinfo",
                ["Method"] = Request.Method,
                ["UserId"] = User.Identity?.Name ?? "unknown"
            }))
            {
                try
                {
                    _logger.LogInformation("Processing user info request");
                    var token = ExtractBearerToken();
                    
                    var authServiceUrl = _config["Services:Auth"];
                    var url = $"{authServiceUrl}/api/auth/userinfo";
                    
                    _logger.LogDebug("Forwarding user info request to: {Url}", url);
                    var response = await _proxy.ForwardAsync(url, Request, token);
                    var content = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation(
                        "User info response - Status: {StatusCode}, ContentLength: {ContentLength}",
                        response.StatusCode,
                        content.Length
                    );
                    
                    return StatusCode((int)response.StatusCode, content);
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    _logger.LogWarning(uaEx, "User info request failed - no valid token provided");
                    return Unauthorized(new 
                    { 
                        error = "No valid authorization token provided",
                        correlationId = correlationId 
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting user info. CorrelationId: {CorrelationId}", correlationId);
                    return StatusCode(500, new 
                    { 
                        error = "Internal server error fetching user information",
                        correlationId = correlationId 
                    });
                }
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var correlationId = Guid.NewGuid().ToString();
            
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["Endpoint"] = "auth/logout",
                ["Method"] = Request.Method,
                ["UserId"] = User.Identity?.Name ?? "unknown"
            }))
            {
                try
                {
                    _logger.LogInformation("Processing logout request");
                    var token = ExtractBearerToken();
                    
                    var authServiceUrl = _config["Services:Auth"];
                    var url = $"{authServiceUrl}/api/auth/logout";
                    
                    _logger.LogDebug("Forwarding logout request to: {Url}", url);
                    var response = await _proxy.ForwardAsync(url, Request, token);
                    var content = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation(
                        "Logout response - Status: {StatusCode}",
                        response.StatusCode
                    );
                    
                    // Clear the JWT cookie
                    _authCookie.ClearJwtCookie(Response);
                    _logger.LogInformation("JWT cookie cleared successfully");
                    
                    return StatusCode((int)response.StatusCode, content);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing logout. CorrelationId: {CorrelationId}", correlationId);
                    return StatusCode(500, new 
                    { 
                        error = "Internal server error during logout",
                        correlationId = correlationId 
                    });
                }
            }
        }

        private string ExtractBearerToken()
        {
            if (Request.Cookies.ContainsKey("access_token"))
            {
                var token = Request.Cookies["access_token"];
                _logger.LogDebug("Extracted token from cookie. Token length: {TokenLength}", token?.Length ?? 0);
                return token ?? string.Empty;
            }
            
            var authHeader = Request.Headers["Authorization"].ToString();
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("No Bearer token found in request headers or cookies");
                throw new UnauthorizedAccessException("No valid authorization token provided");
            }

            var tokenFromHeader = authHeader.Replace("Bearer ", "");
            _logger.LogDebug("Extracted token from Authorization header. Token length: {TokenLength}", tokenFromHeader.Length);
            return tokenFromHeader;
        }
    }
}