using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using auth_service_dotnet.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace auth_service_dotnet.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<JwtService> _logger;
        private readonly SymmetricSecurityKey _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryMinutes;

        public JwtService(IConfiguration config, ILogger<JwtService> logger)
        {
            _config = config;
            _logger = logger;
            
            // Get JWT configuration
            var jwtKey = _config["JWT_SECRET"] ?? 
                        _config["Jwt:Key"] ?? 
                        "default_very_long_secret_key_for_development_only_change_in_production";
            
            _issuer = _config["Jwt:Issuer"] ?? "auth-service";
            _audience = _config["Jwt:Audience"] ?? "api-gateway";
            _expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out var expiry) ? expiry : 60;
            
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            
            _logger.LogInformation($"JWT Service initialized. Issuer: {_issuer}, Audience: {_audience}, Expiry: {_expiryMinutes} minutes");
        }

        public string GenerateToken(string username, string role)
        {
            try
            {
                _logger.LogDebug($"Generating JWT token for user: {username}, role: {role}");
                
                var claims = new[]
                {
                    new Claim("sub", username),
                    new Claim("role", role),
                    new Claim("jti", Guid.NewGuid().ToString()),
                    new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(_expiryMinutes),
                    Issuer = _issuer,
                    Audience = _audience,
                    SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);
                
                _logger.LogInformation($"Token generated successfully for user: {username}");
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating token for user: {username}");
                throw;
            }
        }

        public Dictionary<string, string>? ValidateToken(string token)
        {
            try
            {
                _logger.LogDebug($"Validating JWT token");
                
                var tokenHandler = new JwtSecurityTokenHandler();
                
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _key,
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                
                if (validatedToken is not JwtSecurityToken jwtToken)
                {
                    _logger.LogWarning("Token validation failed: Not a valid JWT token");
                    return null;
                }

                var claims = new Dictionary<string, string>();
                
                foreach (var claim in jwtToken.Claims)
                {
                    if (!claims.ContainsKey(claim.Type))
                    {
                        claims[claim.Type] = claim.Value;
                    }
                }

                _logger.LogDebug($"Token validated successfully for subject: {claims.GetValueOrDefault("sub")}");
                return claims;
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Token validation failed: Token has expired");
                return null;
            }
            catch (SecurityTokenInvalidIssuerException)
            {
                _logger.LogWarning("Token validation failed: Invalid issuer");
                return null;
            }
            catch (SecurityTokenInvalidAudienceException)
            {
                _logger.LogWarning("Token validation failed: Invalid audience");
                return null;
            }
            catch (SecurityTokenSignatureKeyNotFoundException)
            {
                _logger.LogWarning("Token validation failed: Signature key not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return null;
            }
        }

        public string? RefreshToken(string token)
        {
            try
            {
                var claims = ValidateToken(token);
                if (claims == null)
                {
                    _logger.LogWarning("Token refresh failed: Original token is invalid");
                    return null;
                }

                var username = claims.GetValueOrDefault("sub");
                var role = claims.GetValueOrDefault("role");

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(role))
                {
                    _logger.LogWarning("Token refresh failed: Missing required claims");
                    return null;
                }

                _logger.LogInformation($"Refreshing token for user: {username}");
                return GenerateToken(username, role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return null;
            }
        }

        public Dictionary<string, string>? GetTokenClaims(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                
                if (!tokenHandler.CanReadToken(token))
                {
                    _logger.LogWarning("Cannot read token for claims extraction");
                    return null;
                }

                var jwtToken = tokenHandler.ReadJwtToken(token);
                var claims = new Dictionary<string, string>();
                
                foreach (var claim in jwtToken.Claims)
                {
                    if (!claims.ContainsKey(claim.Type))
                    {
                        claims[claim.Type] = claim.Value;
                    }
                }

                return claims;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting token claims");
                return null;
            }
        }

        public DateTime? GetTokenExpiration(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.ValidTo;
            }
            catch
            {
                return null;
            }
        }
    }
}