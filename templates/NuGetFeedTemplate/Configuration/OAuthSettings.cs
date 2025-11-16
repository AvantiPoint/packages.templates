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
    
    /// <summary>
    /// The Google Workspace domain (e.g., "example.com") to restrict authentication to.
    /// Only users with email addresses from this domain will be allowed to authenticate.
    /// This prevents personal Gmail accounts from accessing the feed.
    /// </summary>
    public string WorkspaceDomain { get; set; }
}
