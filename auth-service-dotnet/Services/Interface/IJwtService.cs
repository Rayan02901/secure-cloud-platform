namespace auth_service_dotnet.Services.Interface
{
    public interface IJwtService
    {
        string GenerateToken(string username, string role);
    }
}
