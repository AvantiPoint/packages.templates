namespace NuGetFeedTemplate.Configuration;

public class OAuthSettings
{
    public string Provider { get; set; } = "Microsoft"; // "Microsoft" or "Google"
    
    // Microsoft settings
    public MicrosoftOAuthSettings Microsoft { get; set; }
    
    // Google settings
    public GoogleOAuthSettings Google { get; set; }
}

public class MicrosoftOAuthSettings
{
    public string TenantId { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string Instance { get; set; } = "https://login.microsoftonline.com/";
    public string CallbackPath { get; set; } = "/signin-microsoft";
}

public class GoogleOAuthSettings
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string CallbackPath { get; set; } = "/signin-google";
}
