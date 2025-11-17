using Microsoft.EntityFrameworkCore;
using NuGetFeedTemplate.Configuration;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace NuGetFeedTemplate.Services;

public record OAuthUserInfo(
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    string ProfilePictureUrl);

public interface IOAuthService
{
    bool IsValidProvider(string provider);
    string GetRedirectUri(HttpContext httpContext, string provider);
    string GetAuthUrl(string provider, string redirectUri);
    Task<OAuthUserInfo> GetUserInfoFromProvider(string provider, string code, string redirectUri);
}

public class OAuthService : IOAuthService
{
    private readonly OAuthSettings _oauthSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OAuthService> _logger;

    public OAuthService(
        OAuthSettings oauthSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<OAuthService> logger)
    {
        _oauthSettings = oauthSettings;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public bool IsValidProvider(string provider)
    {
        return provider?.ToLower() == "microsoft" || provider?.ToLower() == "google";
    }

    public string GetRedirectUri(HttpContext httpContext, string provider)
    {
        var scheme = httpContext.Request.Scheme;
        var host = httpContext.Request.Host.ToString();
        var callbackPath = provider.ToLower() == "microsoft"
            ? _oauthSettings.Microsoft?.CallbackPath ?? "/api/authentication/callback/microsoft"
            : _oauthSettings.Google?.CallbackPath ?? "/api/authentication/callback/google";

        return $"{scheme}://{host}{callbackPath}";
    }

    public string GetAuthUrl(string provider, string redirectUri)
    {
        return provider.ToLower() == "microsoft"
            ? GetMicrosoftAuthUrl(redirectUri)
            : GetGoogleAuthUrl(redirectUri);
    }

    public async Task<OAuthUserInfo> GetUserInfoFromProvider(string provider, string code, string redirectUri)
    {
        if (provider.ToLower() == "microsoft")
            return await GetMicrosoftUserInfo(code, redirectUri);
        else
            return await GetGoogleUserInfo(code, redirectUri);
    }

    private string GetMicrosoftAuthUrl(string redirectUri)
    {
        var settings = _oauthSettings.Microsoft;
        var authEndpoint = $"{settings.Instance}{settings.TenantId}/oauth2/v2.0/authorize";

        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = settings.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = redirectUri,
            ["response_mode"] = "query",
            ["scope"] = "openid profile email User.Read",
            ["state"] = Guid.NewGuid().ToString()
        };

        var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        return $"{authEndpoint}?{queryString}";
    }

    private string GetGoogleAuthUrl(string redirectUri)
    {
        var settings = _oauthSettings.Google;
        var authEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";

        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = settings.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = redirectUri,
            ["scope"] = "openid profile email",
            ["state"] = Guid.NewGuid().ToString()
        };

        var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        return $"{authEndpoint}?{queryString}";
    }

    private async Task<OAuthUserInfo> GetMicrosoftUserInfo(string code, string redirectUri)
    {
        var settings = _oauthSettings.Microsoft;
        var tokenEndpoint = $"{settings.Instance}{settings.TenantId}/oauth2/v2.0/token";

        var httpClient = _httpClientFactory.CreateClient();

        // Exchange code for token
        var tokenRequest = new Dictionary<string, string>
        {
            ["client_id"] = settings.ClientId,
            ["client_secret"] = settings.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        };

        var tokenResponse = await httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(tokenRequest));
        tokenResponse.EnsureSuccessStatusCode();

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var accessToken = tokenData["access_token"].ToString();

        // Get user info
        var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var userInfoResponse = await httpClient.SendAsync(userInfoRequest);
        userInfoResponse.EnsureSuccessStatusCode();

        var userData = await userInfoResponse.Content.ReadFromJsonAsync<JsonElement>();

        var email = userData.TryGetProperty("mail", out var mailProp) ? mailProp.GetString()
                  : userData.TryGetProperty("userPrincipalName", out var upnProp) ? upnProp.GetString()
                  : null;
        
        var firstName = userData.TryGetProperty("givenName", out var givenNameProp) ? givenNameProp.GetString() : string.Empty;
        var lastName = userData.TryGetProperty("surname", out var surnameProp) ? surnameProp.GetString() : string.Empty;
        var displayName = userData.TryGetProperty("displayName", out var displayNameProp) ? displayNameProp.GetString() : email;

        // Try to get profile photo URL
        string profilePictureUrl = null;
        try
        {
            var photoRequest = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/photo/$value");
            photoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var photoResponse = await httpClient.SendAsync(photoRequest);
            if (photoResponse.IsSuccessStatusCode)
            {
                // Store the Graph API endpoint as the URL
                var userId = userData.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                if (userId != null)
                {
                    profilePictureUrl = $"https://graph.microsoft.com/v1.0/users/{userId}/photo/$value";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not fetch Microsoft profile photo for user {Email}", email);
        }

        return new OAuthUserInfo(email, firstName, lastName, displayName, profilePictureUrl);
    }

    private async Task<OAuthUserInfo> GetGoogleUserInfo(string code, string redirectUri)
    {
        var settings = _oauthSettings.Google;
        var tokenEndpoint = "https://oauth2.googleapis.com/token";

        var httpClient = _httpClientFactory.CreateClient();

        // Exchange code for token
        var tokenRequest = new Dictionary<string, string>
        {
            ["client_id"] = settings.ClientId,
            ["client_secret"] = settings.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        };

        var tokenResponse = await httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(tokenRequest));
        tokenResponse.EnsureSuccessStatusCode();

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var accessToken = tokenData["access_token"].ToString();

        // Get user info
        var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var userInfoResponse = await httpClient.SendAsync(userInfoRequest);
        userInfoResponse.EnsureSuccessStatusCode();

        var userData = await userInfoResponse.Content.ReadFromJsonAsync<JsonElement>();

        var email = userData.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
        
        // Validate email domain if WorkspaceDomain is configured
        if (!string.IsNullOrEmpty(settings.WorkspaceDomain) && !string.IsNullOrEmpty(email))
        {
            var emailDomain = email.Split('@').LastOrDefault();
            if (!string.Equals(emailDomain, settings.WorkspaceDomain, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("User {Email} attempted to authenticate but is not from allowed domain {Domain}", 
                    email, settings.WorkspaceDomain);
                throw new UnauthorizedAccessException($"Only users from {settings.WorkspaceDomain} domain are allowed to authenticate.");
            }
        }

        // Verify email is verified
        var isVerified = userData.TryGetProperty("verified_email", out var verifiedProp) && verifiedProp.GetBoolean();
        if (!isVerified)
        {
            _logger.LogWarning("User {Email} attempted to authenticate but email is not verified", email);
            throw new UnauthorizedAccessException("Email address must be verified.");
        }

        var firstName = userData.TryGetProperty("given_name", out var givenNameProp) ? givenNameProp.GetString() : string.Empty;
        var lastName = userData.TryGetProperty("family_name", out var familyNameProp) ? familyNameProp.GetString() : string.Empty;
        var displayName = userData.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : email;
        var profilePictureUrl = userData.TryGetProperty("picture", out var pictureProp) ? pictureProp.GetString() : null;

        return new OAuthUserInfo(email, firstName, lastName, displayName, profilePictureUrl);
    }
}
