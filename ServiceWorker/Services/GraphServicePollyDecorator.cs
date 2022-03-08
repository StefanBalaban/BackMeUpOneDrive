using BackMeUp.ServiceWorker.Configurations;
using BackMeUp.ServiceWorker.Interfaces;
using BackMeUp.ServiceWorker.Models;
using Microsoft.Graph;
using Polly;
using Polly.Retry;
using System.Net;

namespace BackMeUp.ServiceWorker.Services
{
    public class GraphServicePollyDecorator : IGraphService
    {
        private readonly IGraphService _inner;
        private readonly ILogger<GraphServicePollyDecorator> _logger;
        private readonly int _rateLimitMiliseconds;
        private readonly AsyncRetryPolicy _retry;

        public GraphServicePollyDecorator(IGraphService inner, NetworkConfiguration networkConfiguration,
            ILogger<GraphServicePollyDecorator> logger)
        {
            _inner = inner;

            // Grap API might incur some of its own throttling 
            _retry = Policy
                .Handle<HttpRequestException>(ex => ex.StatusCode != HttpStatusCode.Unauthorized)
                .Or<ServiceException>()
                .WaitAndRetryAsync(new[] {TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(3)});


            _rateLimitMiliseconds = networkConfiguration.RateLimitInMiliseconds;
            _logger = logger;
        }

        public async Task<Stream> DownloadFileAsync(string id, CancellationToken stoppingToken)
        {
            try
            {
                RateLimit();
                return await _retry.ExecuteAsync(() => _inner.DownloadFileAsync(id, stoppingToken));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Download of file: {fileId} failed due to an HTTP error. HTTP status code: {code}", id,
                    ex.StatusCode);
                throw;
            }
            catch (ServiceException ex)
            {
                _logger.LogError(
                    ex,
                    "Download of file: {fileId} failed due to an Graph API error. " +
                    "HTTP status code: {code}. " +
                    "Raw response body: {raw}",
                    id,
                    ex.StatusCode,
                    ex.RawResponseBody);

                throw;
            }
        }

        public async Task<List<FileDownload>> GetFilesListAsync(CancellationToken stoppingToken)
        {
            try
            {
                return await _retry.ExecuteAsync(async () => await _inner.GetFilesListAsync(stoppingToken));
            }
            catch (ServiceException ex)
            {
                _logger.LogError(
                    ex,
                    "Download of file list failed due to an Graph API error. " +
                    "HTTP status code: {code}. " +
                    "Raw response body: {raw}",
                    ex.StatusCode,
                    ex.RawResponseBody);
                throw;
            }
        }


        private void RateLimit()
        {
            Thread.Sleep(_rateLimitMiliseconds);
        }
    }
}