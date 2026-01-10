using auth_service_dotnet.Controllers;
using auth_service_dotnet.Services;
using auth_service_dotnet.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Collections.Generic;

namespace auth_service_dotnet.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public void FullLoginFlow_ValidCredentials_ReturnsValidToken()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "SUPER_SECRET_JWT_KEY_CHANGE_LATER",
                    ["Jwt:Issuer"] = "SecureCloudPlatform", 
                    ["Jwt:Audience"] = "SecureCloudClients",
                    ["Jwt:ExpiryMinutes"] = "60"
                })
                .Build();

            var loggerMock = new Mock<ILogger<JwtService>>();
            var jwtService = new JwtService(config, loggerMock.Object);
            
            var controllerLoggerMock = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(jwtService, controllerLoggerMock.Object);

            var request = new auth_service_dotnet.Dto.AuthDto.LoginRequest
            {
                Username = "admin",
                Password = "password"
            };

            // Act
            var result = controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value as auth_service_dotnet.Dto.AuthDto.AuthResponse;
            Assert.NotNull(response);
            Assert.NotNull(response.Token);
            
            // Check that token is valid (returns non-null dictionary)
            var claims = jwtService.ValidateToken(response.Token);
            Assert.NotNull(claims);
            Assert.Equal("admin", claims["sub"]);
        }

        [Fact]
        public void Token_Contains_Correct_Claims()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Jwt:Key"] = "YourSuperSecretKeyHereWithMinimum32CharsLong123456",
                    ["Jwt:Issuer"] = "SecureCloudPlatform",
                    ["Jwt:Audience"] = "SecureCloudClients",
                    ["Jwt:ExpiryMinutes"] = "60"
                })
                .Build();

            var loggerMock = new Mock<ILogger<JwtService>>();
            var jwtService = new JwtService(config, loggerMock.Object);

            // Act
            var token = jwtService.GenerateToken("john.doe", "Administrator");

            // Assert
            Assert.NotNull(token);
            
            // Basic JWT structure validation
            var parts = token.Split('.');
            Assert.Equal(3, parts.Length); // Header.Payload.Signature
            
            // Validate the token and check claims
            var claims = jwtService.ValidateToken(token);
            Assert.NotNull(claims);
            Assert.Equal("john.doe", claims["sub"]);
            Assert.Equal("Administrator", claims["role"]);
        }
    }
}