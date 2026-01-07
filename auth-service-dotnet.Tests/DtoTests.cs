using System.ComponentModel.DataAnnotations;
using auth_service_dotnet.Dto;
using Xunit;

namespace auth_service_dotnet.Tests
{
    public class DtoTests
    {
        [Fact]
        public void LoginRequest_ValidData_PassesValidation()
        {
            // Arrange
            var request = new AuthDto.LoginRequest
            {
                Username = "testuser",
                Password = "password123"
            };
            var context = new ValidationContext(request);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(request, context, results, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(results);
        }

        [Fact]
        public void LoginRequest_MissingUsername_FailsValidation()
        {
            // Arrange
            var request = new AuthDto.LoginRequest
            {
                Username = "",  // Empty
                Password = "password123"
            };
            var context = new ValidationContext(request);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(request, context, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Single(results);
            Assert.Contains("Username", results[0].ErrorMessage);
        }
    }
}