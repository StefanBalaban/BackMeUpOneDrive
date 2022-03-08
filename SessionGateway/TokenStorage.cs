using System.Text.Json.Serialization;

namespace BackMeUp.SessionGateway;

public class TokenStorage
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; }

    public string AuthCode { get; set; }

    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; }

    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }

    public DateTime? ValidUntilUtc { get; set; }
}