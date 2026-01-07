using auth_service_dotnet.Controllers;
using auth_service_dotnet.Services.Interface;
using auth_service_dotnet.Dto;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace auth_service_dotnet.Tests
{
    public class AuthControllerTests
    {
        private readonly Mock<IJwtService> _mockJwtService;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockJwtService = new Mock<IJwtService>();
            _controller = new AuthController(_mockJwtService.Object);
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
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public void Login_NullRequest_ReturnsBadRequest()
        {
            // Act
            var result = _controller.Login(null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}