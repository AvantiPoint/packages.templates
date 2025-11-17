namespace NuGetFeedTemplate.Configuration;

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
