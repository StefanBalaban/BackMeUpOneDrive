using BackMeUp.ServiceWorker.Configurations;
using BackMeUp.ServiceWorker.Interfaces;
using Polly;
using Polly.Retry;
using System.Net;

namespace BackMeUp.ServiceWorker.Services
{
    public class SessionServicePollyDecorator : ISessionService
    {
        private readonly ISessionService _inner;
        private readonly ILogger<SessionServicePollyDecorator> _logger;
        private readonly AsyncRetryPolicy _retry;

        public SessionServicePollyDecorator(ISessionService inner, NetworkConfiguration networkConfiguration,
            ILogger<SessionServicePollyDecorator> logger)
        {
            _inner = inner;

            _retry = Policy
                .Handle<HttpRequestException>(ex => ex.StatusCode != HttpStatusCode.Unauthorized)
                .RetryAsync(networkConfiguration.Retries);

            _logger = logger;
        }

        public bool IsValidAccessToken()
        {
            return _inner.IsValidAccessToken();
        }

        public async Task<string> GetAccessTokenAsync(CancellationToken stoppingToken)
        {
            try
            {
                return await _retry.ExecuteAsync(() => _inner.GetAccessTokenAsync(stoppingToken));
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogError("Authentication failed due to expired refresh token");
                }
                else
                {
                    _logger.LogError("Authentication failed due to HTTP error. HTTP status code: {code}",
                        ex.StatusCode);
                }

                throw;
            }
        }
    }
}