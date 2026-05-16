namespace PokenaeTemplate.Authentication;

public class GoogleAuthenticationOptions
{
    public const string SectionName = "Authentication:Google";

    public string ClientId { get; set; } = string.Empty;
    public string TokenInfoEndpoint { get; set; } = "https://www.googleapis.com/oauth2/v3/tokeninfo";
    public string UserInfoEndpoint { get; set; } = "https://openidconnect.googleapis.com/v1/userinfo";
}