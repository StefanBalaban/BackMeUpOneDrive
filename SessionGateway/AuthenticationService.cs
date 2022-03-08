namespace BackMeUp.SessionGateway;

public class AuthenticationService
{
    private readonly AuthenticationConfiguration _authenticationConfiguration;
    private IHttpClientFactory _httpClientFactory;
    private readonly TokenStorage _tokenStorage;

    public AuthenticationService(AuthenticationConfiguration authenticationConfiguration,
        IHttpClientFactory httpClientFactory, TokenStorage tokenStorage)
    {
        _authenticationConfiguration = authenticationConfiguration;
        _httpClientFactory = httpClientFactory;
        _tokenStorage = tokenStorage;
    }

    public async Task FetchAcessTokenFromCode(string code, string state)
    {
        if (string.IsNullOrEmpty(_authenticationConfiguration.State) || _authenticationConfiguration.State != state)
        {
            throw new Exception();
        }

        var client = _httpClientFactory.CreateClient();

        var requestBody = new Dictionary<string, string>();
        requestBody.Add("client_id", _authenticationConfiguration.ClientId);
        requestBody.Add("scope", _authenticationConfiguration.Scope);
        requestBody.Add("redirect_uri", _authenticationConfiguration.RedirectUri);
        requestBody.Add("client_secret", _authenticationConfiguration.ClientSecret);
        requestBody.Add("grant_type", "authorization_code");
        requestBody.Add("code", code);

        using var content = new FormUrlEncodedContent(requestBody);

        // TODO: Add code verifier
        content.Headers.Clear();
        content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

        using var httpResponseMessage = await client.PostAsync(_authenticationConfiguration.TokenEndpoint, content);

        httpResponseMessage.EnsureSuccessStatusCode();

        var result = await httpResponseMessage.Content.ReadFromJsonAsync<TokenStorage>();

        _tokenStorage.AccessToken = result.AccessToken;
        _tokenStorage.RefreshToken = result.RefreshToken;
        _tokenStorage.ValidUntilUtc = DateTime.UtcNow.AddSeconds(result.ExpiresIn);
    }

    internal bool IsValidToken()
    {
        return _tokenStorage != null && _tokenStorage.ValidUntilUtc.HasValue &&
               _tokenStorage.ValidUntilUtc > DateTime.UtcNow;
    }

    private async Task FetchAccessTokenFromRefreshToken()
    {
        if (string.IsNullOrEmpty(_tokenStorage.RefreshToken))
        {
            throw new Exception();
        }

        var client = _httpClientFactory.CreateClient();

        var requestBody = new Dictionary<string, string>();
        requestBody.Add("client_id", _authenticationConfiguration.ClientId);
        requestBody.Add("scope", _authenticationConfiguration.Scope);
        requestBody.Add("redirect_uri", _authenticationConfiguration.RedirectUri);
        requestBody.Add("client_secret", _authenticationConfiguration.ClientSecret);
        requestBody.Add("grant_type", "refresh_token");
        requestBody.Add("refresh_token", _tokenStorage.RefreshToken);

        using var content = new FormUrlEncodedContent(requestBody);

        // TODO: Add code verifier
        content.Headers.Clear();
        content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

        using var httpResponseMessage = await client.PostAsync(_authenticationConfiguration.TokenEndpoint, content);

        // TODO: Message for invalid with special response
        httpResponseMessage.EnsureSuccessStatusCode();

        var result = await httpResponseMessage.Content.ReadFromJsonAsync<TokenStorage>();

        _tokenStorage.AccessToken = result.AccessToken;
        _tokenStorage.RefreshToken = result.RefreshToken;
        _tokenStorage.ValidUntilUtc = DateTime.UtcNow.AddSeconds(result.ExpiresIn);
    }

    public async Task<AccessTokenResponse> GetAccessToken()
    {
        if (!IsValidToken())
        {
            await FetchAccessTokenFromRefreshToken();
        }

        return new AccessTokenResponse
        {
            AccessToken = _tokenStorage.AccessToken, ValidUntilUtc = _tokenStorage.ValidUntilUtc.Value
        };
    }
}