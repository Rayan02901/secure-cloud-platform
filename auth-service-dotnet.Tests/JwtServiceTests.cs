using auth_service_dotnet.Services.Interface;
using auth_service_dotnet.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace auth_service_dotnet.Tests
{
    public class JwtServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ILogger<JwtService>> _mockLogger;
        private readonly IJwtService _jwtService;

        public JwtServiceTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<JwtService>>();

            // Mock ALL required configuration values
            _mockConfig.Setup(x => x["Jwt:Key"]).Returns("YourSuperSecretKeyHereWithMinimum32CharsLong123456");
            _mockConfig.Setup(x => x["Jwt:Issuer"]).Returns("SecureCloudPlatform");
            _mockConfig.Setup(x => x["Jwt:Audience"]).Returns("SecureCloudClients");
            _mockConfig.Setup(x => x["Jwt:ExpiryMinutes"]).Returns("60");

            _jwtService = new JwtService(_mockConfig.Object, _mockLogger.Object);
        }

        [Fact]
        public void GenerateToken_ValidInput_ReturnsToken()
        {
            // Arrange
            var username = "testuser";
            var role = "Admin";

            // Act
            var token = _jwtService.GenerateToken(username, role);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.Contains(".", token); // JWT has 3 parts separated by dots
        }

        [Fact]
        public void GenerateToken_DifferentRoles_ReturnsDifferentTokens()
        {
            // Arrange
            var username = "testuser";

            // Act
            var adminToken = _jwtService.GenerateToken(username, "Admin");
            var userToken = _jwtService.GenerateToken(username, "User");

            // Assert
            Assert.NotEqual(adminToken, userToken);
        }

        [Fact]
        public void GenerateToken_WithNullConfig_StillGeneratesToken()
        {
            // Arrange
            var invalidConfig = new Mock<IConfiguration>();
            var logger = new Mock<ILogger<JwtService>>();
            
            invalidConfig.Setup(x => x["Jwt:Key"]).Returns((string?)null);
            invalidConfig.Setup(x => x["Jwt:Issuer"]).Returns((string?)null);
            invalidConfig.Setup(x => x["Jwt:Audience"]).Returns((string?)null);
            invalidConfig.Setup(x => x["Jwt:ExpiryMinutes"]).Returns((string?)null);
            
            var invalidService = new JwtService(invalidConfig.Object, logger.Object);

            // Act
            var token = invalidService.GenerateToken("test", "Admin");
            var isValid = invalidService.ValidateToken(token) != null;

            // Assert - Service generates a valid token even with null config
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.True(isValid); // Token is valid
        }

        [Fact]
        public void GenerateToken_EmptyUsername_DoesNotThrow()
        {
            // Act
            var token = _jwtService.GenerateToken("", "Admin");
            
            // Assert - Should generate token even with empty username
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public void GenerateToken_EmptyRole_DoesNotThrow()
        {
            // Act
            var token = _jwtService.GenerateToken("testuser", "");
            
            // Assert - Should generate token even with empty role
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public void GenerateToken_SameInput_ReturnsDifferentTokens()
        {
            // Arrange
            var username = "testuser";
            var role = "Admin";

            // Act
            var token1 = _jwtService.GenerateToken(username, role);
            var token2 = _jwtService.GenerateToken(username, role);

            // Assert - JWTs with different JTI (JWT ID) should be different
            // This is expected behavior since each token has unique JTI
            Assert.NotEqual(token1, token2);
        }

        [Fact]
        public void ValidateToken_ValidToken_ReturnsClaimsDictionary()
        {
            // Arrange
            var username = "testuser";
            var role = "Admin";
            var token = _jwtService.GenerateToken(username, role);

            // Act
            var claims = _jwtService.ValidateToken(token);

            // Assert
            Assert.NotNull(claims); // Valid token returns non-null dictionary
            Assert.NotEmpty(claims);
            Assert.Equal(username, claims["sub"]); // Check subject claim
            Assert.Equal(role, claims["role"]); // Check role claim
        }

        [Fact]
        public void ValidateToken_InvalidToken_ReturnsNull()
        {
            // Arrange
            var invalidToken = "invalid.token.here";

            // Act
            var claims = _jwtService.ValidateToken(invalidToken);

            // Assert
            Assert.Null(claims); // Invalid token returns null
        }

        [Fact]
        public void ValidateToken_ExpiredToken_ReturnsNull()
        {
            // Arrange - Create a token that expired 1 hour AGO
            var expiredConfig = new Mock<IConfiguration>();
            expiredConfig.Setup(x => x["Jwt:Key"]).Returns("YourSuperSecretKeyHereWithMinimum32CharsLong123456");
            expiredConfig.Setup(x => x["Jwt:Issuer"]).Returns("SecureCloudPlatform");
            expiredConfig.Setup(x => x["Jwt:Audience"]).Returns("SecureCloudClients");
            
            // Get a JwtService instance with normal expiry
            var tempService = new JwtService(expiredConfig.Object, _mockLogger.Object);
            var token = tempService.GenerateToken("test", "Admin");
            
            // Decode the token to see its structure
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            // Now create a token with PAST expiration manually
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("sub", "test"),
                    new Claim("role", "Admin"),
                    new Claim("jti", Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(-1), // Expired 1 hour ago
                NotBefore = DateTime.UtcNow.AddHours(-2), // Valid starting 2 hours ago
                IssuedAt = DateTime.UtcNow.AddHours(-2),
                Issuer = "SecureCloudPlatform",
                Audience = "SecureCloudClients",
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSuperSecretKeyHereWithMinimum32CharsLong123456")),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            
            var expiredToken = handler.CreateToken(tokenDescriptor);
            var expiredTokenString = handler.WriteToken(expiredToken);

            // Act
            var claims = _jwtService.ValidateToken(expiredTokenString);

            // Assert
            Assert.Null(claims); // Expired token should return null
        }

        [Fact]
        public void ValidateToken_ManipulatedToken_ReturnsNull()
        {
            // Arrange
            var token = _jwtService.GenerateToken("testuser", "Admin");
            var parts = token.Split('.');
            
            // Manipulate the signature
            var manipulatedToken = $"{parts[0]}.{parts[1]}.INVALID_SIGNATURE";

            // Act
            var claims = _jwtService.ValidateToken(manipulatedToken);

            // Assert
            Assert.Null(claims); // Tampered token returns null
        }
    }
}