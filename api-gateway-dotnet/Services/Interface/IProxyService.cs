namespace api_gateway_dotnet.Services.Interface
{
    public interface IProxyService
    {
        Task<HttpResponseMessage> ForwardAsync(string url, HttpRequest request, string token);
    }
}
