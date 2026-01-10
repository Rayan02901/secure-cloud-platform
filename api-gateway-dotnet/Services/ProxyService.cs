using api_gateway_dotnet.Services.Interface;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace api_gateway_dotnet.Services
{
    public class ProxyService : IProxyService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<ProxyService> _logger;

        public ProxyService(
            IHttpClientFactory clientFactory,
            ILogger<ProxyService> logger
        )
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<HttpResponseMessage> ForwardAsync(
            string url,
            HttpRequest request,
            string? token = null
        )
        {
            try
            {
                _logger.LogDebug($"Forwarding {request.Method} request to: {url}");
                
                var client = _clientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                
                var forwardRequest = new HttpRequestMessage(
                    new HttpMethod(request.Method),
                    url
                );

                // Add authorization header if token is provided
                if (!string.IsNullOrEmpty(token))
                {
                    forwardRequest.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                    _logger.LogDebug("Added Bearer token to forwarded request");
                }

                // Copy content type
                if (request.ContentType != null)
                {
                    forwardRequest.Content = new StreamContent(request.Body);
                    forwardRequest.Content.Headers.ContentType = 
                        new MediaTypeHeaderValue(request.ContentType);
                }

                // For GET requests, we don't need to copy body
                if (request.Method != "GET" && request.ContentLength > 0)
                {
                    using var reader = new StreamReader(request.Body, Encoding.UTF8);
                    var body = await reader.ReadToEndAsync();
                    
                    if (!string.IsNullOrEmpty(body))
                    {
                        forwardRequest.Content = new StringContent(
                            body,
                            Encoding.UTF8,
                            request.ContentType ?? "application/json"
                        );
                        _logger.LogDebug($"Request body length: {body.Length} characters");
                    }
                }

                // Copy important headers
                CopyHeaderIfExists(request, forwardRequest, "X-Forwarded-For");
                CopyHeaderIfExists(request, forwardRequest, "User-Agent");
                CopyHeaderIfExists(request, forwardRequest, "Accept");
                CopyHeaderIfExists(request, forwardRequest, "Accept-Language");

                _logger.LogInformation($"Forwarding request to {url}");
                var response = await client.SendAsync(forwardRequest);
                
                _logger.LogDebug($"Response from {url}: {(int)response.StatusCode}");
                
                // Log 4xx and 5xx errors
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Error response from {url}: {(int)response.StatusCode} - {errorContent}");
                }

                return response;
            }
            catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, $"Timeout while forwarding request to {url}");
                return new HttpResponseMessage(System.Net.HttpStatusCode.GatewayTimeout)
                {
                    Content = new StringContent($"Request to {url} timed out")
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"HTTP error while forwarding to {url}");
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadGateway)
                {
                    Content = new StringContent($"Error forwarding request: {ex.Message}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error forwarding to {url}");
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent($"Internal server error: {ex.Message}")
                };
            }
        }

        private void CopyHeaderIfExists(HttpRequest source, HttpRequestMessage destination, string headerName)
        {
            if (source.Headers.TryGetValue(headerName, out var values))
            {
                destination.Headers.TryAddWithoutValidation(headerName, values.ToArray());
            }
        }
    }
}