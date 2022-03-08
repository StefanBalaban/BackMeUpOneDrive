namespace BackMeUp.SessionGateway
{
    public class AccessTokenResponse
    {
        public string AccessToken { get; set; }
        public DateTime ValidUntilUtc { get; set; }
    }
}