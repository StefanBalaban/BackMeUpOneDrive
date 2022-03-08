using System.Globalization;

namespace BackMeUp.ServiceWorker.Configurations;

public class AuthenticationConfiguration
{
    public string TenantId { get; set; }
    public string Instance { get; set; } = "https://login.microsoftonline.com/{0}";

    public string Authority => String.Format(CultureInfo.InvariantCulture, Instance, TenantId);

    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
}