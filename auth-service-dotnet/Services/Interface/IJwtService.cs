using System.Collections.Generic;

namespace auth_service_dotnet.Services.Interface
{
    public interface IJwtService
    {
        string GenerateToken(string username, string role);
        
        Dictionary<string, string>? ValidateToken(string token);
        
        string? RefreshToken(string token);
        
        Dictionary<string, string>? GetTokenClaims(string token);
    }
}