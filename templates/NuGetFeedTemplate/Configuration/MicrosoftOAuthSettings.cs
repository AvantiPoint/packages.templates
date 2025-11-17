namespace NuGetFeedTemplate.Configuration;

public class MicrosoftOAuthSettings
{
    public string TenantId { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string Instance { get; set; } = "https://login.microsoftonline.com/";
    public string CallbackPath { get; set; } = "/signin-microsoft";
}
