using auth_service_dotnet.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using auth_service_dotnet.Dto;
using static auth_service_dotnet.Dto.AuthDto;

namespace auth_service_dotnet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IJwtService jwtService,
            ILogger<AuthController> logger)
        {
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            _logger.LogInformation("Health check endpoint called");
            return Ok(new HealthResponse
            {
                Status = "healthy",
                Service = "auth-service",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0"
            });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            _logger.LogInformation($"Login attempt for username: {req?.Username}");
            
            if (req == null)
            {
                _logger.LogWarning("Login request was null");
                return BadRequest(new ErrorResponse 
                { 
                    Error = "Login request cannot be null" 
                });
            }

            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            {
                _logger.LogWarning("Login attempt with empty username or password");
                return BadRequest(new ErrorResponse 
                { 
                    Error = "Username and password are required" 
                });
            }

            // TEMP: Hardcoded user (will be replaced with database later)
            if (req.Username != "admin" || req.Password != "password")
            {
                _logger.LogWarning($"Failed login attempt for username: {req.Username}");
                return Unauthorized(new ErrorResponse 
                { 
                    Error = "Invalid username or password" 
                });
            }

            try
            {
                var token = _jwtService.GenerateToken(req.Username, "Admin");
                _logger.LogInformation($"Successful login for user: {req.Username}");

                var response = new AuthResponse
                {
                    Token = token,
                    Username = req.Username,
                    Role = "Admin",
                    ExpiresIn = 3600 // 1 hour in seconds
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token");
                return StatusCode(500, new ErrorResponse 
                { 
                    Error = "Internal server error during authentication",
                    Details = ex.Message
                });
            }
        }

        [HttpPost("verify")]
        public IActionResult VerifyToken()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("Token verification failed - no Bearer token");
                return Unauthorized(new ErrorResponse 
                { 
                    Error = "No valid authorization token provided" 
                });
            }

            var token = authHeader.Replace("Bearer ", "");
            
            try
            {
                var claims = _jwtService.ValidateToken(token);
                if (claims == null)
                {
                    _logger.LogWarning("Token verification failed - invalid token");
                    return Unauthorized(new ErrorResponse 
                    { 
                        Error = "Invalid or expired token" 
                    });
                }

                _logger.LogInformation($"Token verified for user: {claims["sub"]}");
                return Ok(new VerifyResponse
                {
                    IsValid = true,
                    Username = claims["sub"],
                    Role = claims["role"],
                    Expires = claims["exp"]
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying token");
                return StatusCode(500, new ErrorResponse 
                { 
                    Error = "Internal server error during token verification",
                    Details = ex.Message
                });
            }
        }

        [HttpPost("refresh")]
        public IActionResult RefreshToken()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var token = authHeader.Replace("Bearer ", "");

            try
            {
                var newToken = _jwtService.RefreshToken(token);
                if (string.IsNullOrEmpty(newToken))
                {
                    _logger.LogWarning("Token refresh failed - invalid original token");
                    return Unauthorized(new ErrorResponse 
                    { 
                        Error = "Invalid or expired token" 
                    });
                }

                var claims = _jwtService.ValidateToken(newToken);
                if (claims == null)
                {
                    _logger.LogError("Generated new token is invalid - this should not happen");
                    return StatusCode(500, new ErrorResponse 
                    { 
                        Error = "Internal server error" 
                    });
                }

                _logger.LogInformation($"Token refreshed for user: {claims["sub"]}");
                
                return Ok(new AuthResponse
                {
                    Token = newToken,
                    Username = claims["sub"],
                    Role = claims["role"],
                    ExpiresIn = 3600
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new ErrorResponse 
                { 
                    Error = "Internal server error during token refresh",
                    Details = ex.Message
                });
            }
        }

        [HttpGet("userinfo")]
        public IActionResult GetUserInfo()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var token = authHeader.Replace("Bearer ", "");

            try
            {
                var claims = _jwtService.GetTokenClaims(token);
                if (claims == null || !claims.ContainsKey("sub"))
                {
                    return Unauthorized(new ErrorResponse 
                    { 
                        Error = "Invalid or expired token" 
                    });
                }

                _logger.LogInformation($"User info requested for: {claims["sub"]}");
                
                return Ok(new UserInfoResponse
                {
                    Username = claims["sub"],
                    Role = claims["role"],
                    IssuedAt = claims.GetValueOrDefault("iat"),
                    ExpiresAt = claims.GetValueOrDefault("exp")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info");
                return StatusCode(500, new ErrorResponse 
                { 
                    Error = "Internal server error fetching user information",
                    Details = ex.Message
                });
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var token = authHeader.Replace("Bearer ", "");

            try
            {
                var claims = _jwtService.GetTokenClaims(token);
                if (claims != null && claims.ContainsKey("sub"))
                {
                    _logger.LogInformation($"User logged out: {claims["sub"]}");
                }

                // In a real application, add token to blacklist here

                return Ok(new LogoutResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new ErrorResponse 
                { 
                    Error = "Internal server error during logout",
                    Details = ex.Message
                });
            }
        }
    }
}