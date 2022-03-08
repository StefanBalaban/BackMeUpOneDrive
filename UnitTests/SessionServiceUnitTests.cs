using BackMeUp.ServiceWorker.Configurations;
using BackMeUp.ServiceWorker.Services;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BackMeUp.UnitTests
{
    public class SessionServiceUnitTests
    {
        [Fact]
        public async Task IsValidSessionToken_CalledWithValidToken_ReturnsTrue()
        {
            SessionConfiguration? sessionConfig = new SessionConfiguration
            {
                AccessToken = "",
                ValidUntilUtc = DateTime.Now.AddHours(1),
                SessionGatewayUrl = "http://localhost:7133/access-token"
            };

            MockHttpMessageHandler? mockHttp = new MockHttpMessageHandler();

            mockHttp.When(sessionConfig.SessionGatewayUrl)
                .Respond("application/json",
                    "{\"accessToken\":\"test\",\"expiresIn\":\"2022-03-07T11:32:56.3497063Z\"}");

            HttpClient? client = new HttpClient(mockHttp);

            Mock<ILogger<SessionService>>? logger = new Mock<ILogger<SessionService>>(MockBehavior.Loose);

            SessionService? sessionService = new SessionService(sessionConfig, client, logger.Object);

            Assert.True(sessionService.IsValidAccessToken());
        }

        [Fact]
        public async Task IsValidSessionToken_CalledWithInvalidToken_ReturnsFalse()
        {
            SessionConfiguration? sessionConfig = new SessionConfiguration
            {
                AccessToken = "",
                ValidUntilUtc = DateTime.Now.AddHours(-1),
                SessionGatewayUrl = "http://localhost:7133/access-token"
            };

            MockHttpMessageHandler? mockHttp = new MockHttpMessageHandler();

            mockHttp.When(sessionConfig.SessionGatewayUrl)
                .Respond("application/json", $"{{accessToken:test, expiresIn:{DateTime.UtcNow.AddHours(1)}}}");

            HttpClient? client = new HttpClient(mockHttp);

            Mock<ILogger<SessionService>>? logger = new Mock<ILogger<SessionService>>(MockBehavior.Loose);

            SessionService? sessionService = new SessionService(sessionConfig, client, logger.Object);

            Assert.False(sessionService.IsValidAccessToken());
        }

        [Fact]
        public async Task GetAccessTokenAsync_Called_ReturnsAccessToken()
        {
            SessionConfiguration? sessionConfig = new SessionConfiguration
            {
                AccessToken = "",
                ValidUntilUtc = DateTime.Now.AddHours(-1),
                SessionGatewayUrl = "http://localhost:7133/access-token"
            };

            MockHttpMessageHandler? mockHttp = new MockHttpMessageHandler();

            mockHttp.When(sessionConfig.SessionGatewayUrl)
                .Respond("application/json",
                    "{\"accessToken\":\"test\",\"expiresIn\":\"2022-03-07T11:32:56.3497063Z\"}");

            HttpClient? client = new HttpClient(mockHttp);

            Mock<ILogger<SessionService>>? logger = new Mock<ILogger<SessionService>>(MockBehavior.Loose);

            SessionService? sessionService = new SessionService(sessionConfig, client, logger.Object);

            Assert.Equal("test", await sessionService.GetAccessTokenAsync(new CancellationToken()));
        }
    }
}