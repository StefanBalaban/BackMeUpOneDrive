using System.Globalization;
using System.Web;

public class AuthenticationConfiguration
{
    public readonly string ClientId;
    public readonly string ClientSecret;

    public readonly string Instance = "https://login.microsoftonline.com/{0}";

    // TODO: Move to config 
    public readonly string RedirectEndpoint;
    public readonly string RedirectUri;
    public readonly string Scope = "user.read files.read files.read.all offline_access";
    public readonly string TenantId;

    public AuthenticationConfiguration(string tenantId, string clientId, string clientSecret, string redirectEndpoint,
        string redirectUri, string scope)
    {
        TenantId = tenantId;
        ClientId = clientId;
        ClientSecret = clientSecret;
        RedirectEndpoint = redirectEndpoint;
        RedirectUri = redirectUri;
        Scope = $"https://graph.microsoft.com/{scope}";
        State = "";
    }

    public string State { get; set; }

    public string Authority => string.Format(CultureInfo.InvariantCulture, Instance, TenantId);

    public string RequestEndpoint
    {
        get
        {
            State = new Random().Next(10000).ToString();

            return $"{Authority}/oauth2/v2.0/authorize?" +
                   $"client_id={ClientId}&" +
                   "response_type=code&" +
                   $"redirect_uri={HttpUtility.UrlEncode(RedirectUri)}&" +
                   "response_mode=query&" +
                   $"scope={HttpUtility.UrlEncode(Scope)}&" +
                   $"state={State}";
        }
    }

    public string TokenEndpoint => $"{Authority}/oauth2/v2.0/token";
}