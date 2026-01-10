using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace auth_service_dotnet.Dto
{
    public class AuthDto
    {
        public class LoginRequest
        {
            [Required]
            [JsonPropertyName("username")]
            public string Username { get; set; } = null!;
            
            [Required]
            [JsonPropertyName("password")]
            public string Password { get; set; } = null!;
        }

        public class AuthResponse
        {
            [Required]
            [JsonPropertyName("token")]
            public string Token { get; set; } = null!;
            
            [JsonPropertyName("username")]
            public string Username { get; set; } = null!;
            
            [JsonPropertyName("role")]
            public string Role { get; set; } = null!;
            
            [JsonPropertyName("expiresIn")]
            public int ExpiresIn { get; set; }
        }

        public class VerifyResponse
        {
            [JsonPropertyName("isValid")]
            public bool IsValid { get; set; }
            
            [JsonPropertyName("username")]
            public string? Username { get; set; }
            
            [JsonPropertyName("role")]
            public string? Role { get; set; }
            
            [JsonPropertyName("expires")]
            public string? Expires { get; set; }
        }

        public class UserInfoResponse
        {
            [JsonPropertyName("username")]
            public string Username { get; set; } = null!;
            
            [JsonPropertyName("role")]
            public string Role { get; set; } = null!;
            
            [JsonPropertyName("issuedAt")]
            public string? IssuedAt { get; set; }
            
            [JsonPropertyName("expiresAt")]
            public string? ExpiresAt { get; set; }
        }

        public class ErrorResponse
        {
            [JsonPropertyName("error")]
            public string Error { get; set; } = null!;
            
            [JsonPropertyName("details")]
            public string? Details { get; set; }
        }

        public class HealthResponse
        {
            [JsonPropertyName("status")]
            public string Status { get; set; } = "healthy";
            
            [JsonPropertyName("service")]
            public string Service { get; set; } = "auth-service";
            
            [JsonPropertyName("timestamp")]
            public DateTime Timestamp { get; set; }
            
            [JsonPropertyName("version")]
            public string Version { get; set; } = "1.0.0";
        }

        public class LogoutResponse
        {
            [JsonPropertyName("message")]
            public string Message { get; set; } = "Successfully logged out";
        }
    }
}