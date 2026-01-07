using auth_service_dotnet.Services.Interface;
using auth_service_dotnet.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace auth_service_dotnet.Tests
{
    public class JwtServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly IJwtService _jwtService;

        public JwtServiceTests()
        {
            _mockConfig = new Mock<IConfiguration>();

            // Mock ALL required configuration values
            _mockConfig.Setup(x => x["Jwt:Key"]).Returns("YourSuperSecretKeyHereWithMinimum32CharsLong");
            _mockConfig.Setup(x => x["Jwt:Issuer"]).Returns("your-issuer");
            _mockConfig.Setup(x => x["Jwt:Audience"]).Returns("your-audience");
            _mockConfig.Setup(x => x["Jwt:ExpiryMinutes"]).Returns("60"); // Must be a string!

            _jwtService = new JwtService(_mockConfig.Object);
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
            // Optional: Verify it's a valid JWT format
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

            // Assert - tokens should be different for different roles
            Assert.NotEqual(adminToken, userToken);
        }

        [Fact]
        public void GenerateToken_WithNullConfig_ThrowsException()
        {
            // Arrange
            var invalidConfig = new Mock<IConfiguration>();
            // Don't setup any values - they'll all return null
            var invalidService = new JwtService(invalidConfig.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                invalidService.GenerateToken("test", "Admin"));
        }
        //[Fact]
        //public void ValidateToken_ValidToken_ReturnsTrue()
        //{
        //    // Arrange
        //    var username = "testuser";
        //    var role = "Admin";
        //    var token = _jwtService.GenerateToken(username, role);

        //    // Act
        //    var isValid = _jwtService.ValidateToken(token);

        //    // Assert
        //    Assert.True(isValid);
        //}
    }
}