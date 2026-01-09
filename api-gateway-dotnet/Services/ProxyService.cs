using api_gateway_dotnet.Services.Interface;
using System.Net.Http.Headers;
namespace api_gateway_dotnet.Services
{


    public class ProxyService : IProxyService
    {
        private readonly IHttpClientFactory _clientFactory;

        public ProxyService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<HttpResponseMessage> ForwardAsync(
            string url,
            HttpRequest request,
            string token
        )
        {
            var client = _clientFactory.CreateClient();
            var forwardRequest = new HttpRequestMessage(
                new HttpMethod(request.Method),
                url
            );

            forwardRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            using var stream = new StreamReader(request.Body);
            var body = await stream.ReadToEndAsync();

            if (!string.IsNullOrEmpty(body))
                forwardRequest.Content =
                    new StringContent(body, System.Text.Encoding.UTF8, "application/json");

            return await client.SendAsync(forwardRequest);
        }
    }

}
