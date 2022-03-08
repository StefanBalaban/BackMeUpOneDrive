using System;

namespace BackMeUp.ServiceWorker.Configurations
{
    public class SessionConfiguration
    {
        public string SessionGatewayUrl { get; set; }
        public string? AccessToken { get; set; }
        public DateTime? ValidUntilUtc { get; set; }
    }
}