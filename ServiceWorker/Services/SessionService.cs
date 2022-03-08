using BackMeUp.ServiceWorker.Configurations;
using BackMeUp.ServiceWorker.Interfaces;
using System.Net.Http.Json;
namespace BackMeUp.ServiceWorker.Services
{
    public class SessionService : ISessionService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SessionService> _logger;
        private readonly SessionConfiguration _sessionConfiguration;

        public SessionService(SessionConfiguration sessionConfiguration, HttpClient httpClient,
            ILogger<SessionService> logger)
        {
            _sessionConfiguration = sessionConfiguration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public bool IsValidAccessToken()
        {
            return _sessionConfiguration.ValidUntilUtc != null &&
                   _sessionConfiguration.ValidUntilUtc.Value > DateTime.UtcNow;
        }

        // TODO Handle failed auth
        private async Task AuthenticateAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(_sessionConfiguration.SessionGatewayUrl))
            {
                // TODO log method, date whatever
                throw new Exception("Session Gateway URL not set.");
            }

            if (_sessionConfiguration.ValidUntilUtc != null &&
                _sessionConfiguration.ValidUntilUtc.Value > DateTime.UtcNow)
            {
                return;
            }


            var httpResponseMessage =
                await _httpClient.GetAsync(_sessionConfiguration.SessionGatewayUrl, stoppingToken);

            httpResponseMessage.EnsureSuccessStatusCode();

            var result = await httpResponseMessage.Content.ReadFromJsonAsync<SessionConfiguration>();

            _sessionConfiguration.AccessToken = result.AccessToken;
            _sessionConfiguration.ValidUntilUtc = result.ValidUntilUtc;
        }

        public async Task<string> GetAccessTokenAsync(CancellationToken stoppingToken)
        {
            await AuthenticateAsync(stoppingToken);

            _logger.LogInformation("Successfully authenticated with session gateway");

            return _sessionConfiguration.AccessToken;
        }
    }
}