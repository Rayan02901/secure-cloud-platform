using System.ComponentModel.DataAnnotations;

namespace auth_service_dotnet.Dto
{
    public class AuthDto
    {
        public class LoginRequest
        {
            [Required]
            public string Username { get; set; } = null!;
            [Required]
            public string Password { get; set; } = null!;
        }
        public class AuthResponse
        {
            [Required]
            public string Token { get; set; } = null!;
        }
    }
    
}
