using System.Net.Http.Headers;
using NuGetFeedTemplate.Configuration;

namespace NuGetFeedTemplate.Services;

public class GraphProfilePhotoService : IGraphProfilePhotoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OAuthSettings _oauthSettings;
    private readonly ILogger<GraphProfilePhotoService> _logger;

    public GraphProfilePhotoService(
        IHttpClientFactory httpClientFactory,
        OAuthSettings oauthSettings,
        ILogger<GraphProfilePhotoService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _oauthSettings = oauthSettings;
        _logger = logger;
    }

    public async Task<Stream> GetCurrentUserPhotoAsync()
    {
        // This method is deprecated for JWT authentication
        // Profile photos should be fetched using the user's email
        _logger.LogWarning("GetCurrentUserPhotoAsync is not supported with JWT authentication");
        return null;
    }

    public async Task<Stream> GetUserPhotoAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Empty email provided for user photo lookup");
            return null;
        }

        try
        {
            // Generate a default avatar instead of fetching from external providers
            // This simplifies the implementation and removes dependency on Graph API
            return await GenerateDefaultAvatarAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate user photo for {Email}", email);
            return null;
        }
    }

    private async Task<Stream> GenerateDefaultAvatarAsync(string email)
    {
        // Generate a simple avatar with initials
        // You can replace this with a library like DiceBear or use Gravatar
        var httpClient = _httpClientFactory.CreateClient();
        
        // Use Gravatar as a fallback
        var hash = ComputeMd5Hash(email.Trim().ToLower());
        var gravatarUrl = $"https://www.gravatar.com/avatar/{hash}?d=identicon&s=200";
        
        try
        {
            var response = await httpClient.GetAsync(gravatarUrl);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Gravatar for {Email}", email);
        }

        return null;
    }

    private static string ComputeMd5Hash(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        
        var sb = new System.Text.StringBuilder();
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
