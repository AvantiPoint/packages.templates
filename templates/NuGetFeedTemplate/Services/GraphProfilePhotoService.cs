using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using NuGetFeedTemplate.Configuration;
using NuGetFeedTemplate.Data;

namespace NuGetFeedTemplate.Services;

public class GraphProfilePhotoService : IGraphProfilePhotoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OAuthSettings _oauthSettings;
    private readonly FeedContext _dbContext;
    private readonly ILogger<GraphProfilePhotoService> _logger;
    private readonly IWebHostEnvironment _environment;

    public GraphProfilePhotoService(
        IHttpClientFactory httpClientFactory,
        OAuthSettings oauthSettings,
        FeedContext dbContext,
        ILogger<GraphProfilePhotoService> logger,
        IWebHostEnvironment environment)
    {
        _httpClientFactory = httpClientFactory;
        _oauthSettings = oauthSettings;
        _dbContext = dbContext;
        _logger = logger;
        _environment = environment;
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
            // Check if we have a cached profile picture
            var cachedImagePath = Path.Combine(_environment.WebRootPath, "profile-cache", $"{email}.jpg");
            if (File.Exists(cachedImagePath))
            {
                return File.OpenRead(cachedImagePath);
            }

            // Get user from database to check for profile picture URL
            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email);
            
            Stream photoStream = null;

            // Try to fetch from the provider's profile picture URL based on configured provider
            if (user != null && !string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                photoStream = await FetchProfilePictureFromUrl(user.ProfilePictureUrl, _oauthSettings.Provider);
            }

            // Fallback to Gravatar if no profile picture from provider
            if (photoStream == null)
            {
                photoStream = await FetchGravatarAsync(email);
            }

            // Cache the photo if we got one
            if (photoStream != null)
            {
                await CacheProfilePictureAsync(email, photoStream);
                // Reopen the cached file to return
                photoStream?.Dispose();
                return File.OpenRead(cachedImagePath);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve user photo for {Email}", email);
            return null;
        }
    }

    private async Task<Stream> FetchProfilePictureFromUrl(string url, string provider)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();

            // For Microsoft Graph URLs, we may need authentication
            if (url.Contains("graph.microsoft.com"))
            {
                _logger.LogDebug("Microsoft Graph profile picture URL detected, but we don't have access token. Skipping.");
                return null;
            }

            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch profile picture from {Provider} URL: {Url}", provider, url);
        }

        return null;
    }

    private async Task<Stream> FetchGravatarAsync(string email)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var hash = ComputeMd5Hash(email.Trim().ToLower());
            var gravatarUrl = $"https://www.gravatar.com/avatar/{hash}?d=identicon&s=200";

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

    private async Task CacheProfilePictureAsync(string email, Stream photoStream)
    {
        try
        {
            var cacheDirectory = Path.Combine(_environment.WebRootPath, "profile-cache");
            Directory.CreateDirectory(cacheDirectory);

            var cachedImagePath = Path.Combine(cacheDirectory, $"{email}.jpg");

            using var fileStream = File.Create(cachedImagePath);
            photoStream.Position = 0;
            await photoStream.CopyToAsync(fileStream);

            _logger.LogInformation("Cached profile picture for {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache profile picture for {Email}", email);
        }
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
