using auth_service_dotnet.Controllers;
using auth_service_dotnet.Services.Interface;
using auth_service_dotnet.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace auth_service_dotnet.Tests
{
    public class AuthControllerTests
    {
        private readonly Mock<IJwtService> _mockJwtService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockJwtService = new Mock<IJwtService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _controller = new AuthController(_mockJwtService.Object, _mockLogger.Object);
        }

        [Fact]
        public void Login_ValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var request = new AuthDto.LoginRequest
            {
                Username = "admin",
                Password = "password"
            };

            var expectedToken = "fake-jwt-token";
            _mockJwtService.Setup(x => x.GenerateToken("admin", "Admin"))
                .Returns(expectedToken);

            // Act
            var result = _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AuthDto.AuthResponse>(okResult.Value);
            Assert.Equal(expectedToken, response.Token);
        }

        [Fact]
        public void Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var request = new AuthDto.LoginRequest
            {
                Username = "wrong",
                Password = "wrong"
            };

            // Act
            var result = _controller.Login(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result); // Changed to UnauthorizedObjectResult
            Assert.NotNull(unauthorizedResult.Value);
        }

        [Fact]
        public void Login_NullRequest_ReturnsBadRequest()
        {
            // Act
            var result = _controller.Login(null!);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public void Login_ValidUser_ValidPasswordFormat()
        {
            // Arrange - Use the exact credentials your controller accepts
            var request = new AuthDto.LoginRequest
            {
                Username = "admin",  // Must be "admin"
                Password = "password"  // Must be "password"
            };

            var expectedToken = "fake-jwt-token";
            _mockJwtService.Setup(x => x.GenerateToken("admin", "Admin"))
                .Returns(expectedToken);

            // Act
            var result = _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AuthDto.AuthResponse>(okResult.Value);
            Assert.Equal(expectedToken, response.Token);
        }

        [Fact]
        public void Login_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new AuthDto.LoginRequest
            {
                Username = "admin",
                Password = "password"
            };

            _mockJwtService.Setup(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Service error"));

            // Act
            var result = _controller.Login(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
    }
}