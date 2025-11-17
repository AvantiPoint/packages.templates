namespace NuGetFeedTemplate.Configuration;

public class OAuthSettings
{
    public string Provider { get; set; } = "Microsoft"; // "Microsoft" or "Google"
    
    // Microsoft settings
    public MicrosoftOAuthSettings Microsoft { get; set; }
    
    // Google settings
    public GoogleOAuthSettings Google { get; set; }
}
