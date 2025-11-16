using Microsoft.EntityFrameworkCore;
using NuGetFeedTemplate.Configuration;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;
using System.Net.Http.Headers;

namespace NuGetFeedTemplate.Services;

public interface IOAuthService
{
    bool IsValidProvider(string provider);
    string GetRedirectUri(HttpContext httpContext, string provider);
    string GetAuthUrl(string provider, string redirectUri);
    Task<(string email, string name, string externalId)> GetUserInfoFromProvider(string provider, string code, string redirectUri);
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

    public async Task<(string email, string name, string externalId)> GetUserInfoFromProvider(string provider, string code, string redirectUri)
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

    private async Task<(string email, string name, string externalId)> GetMicrosoftUserInfo(string code, string redirectUri)
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

        var userData = await userInfoResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        var email = userData.ContainsKey("mail") ? userData["mail"]?.ToString()
                  : userData.ContainsKey("userPrincipalName") ? userData["userPrincipalName"]?.ToString()
                  : null;
        var name = userData.ContainsKey("displayName") ? userData["displayName"]?.ToString() : email;
        var externalId = userData["id"]?.ToString();

        return (email, name, externalId);
    }

    private async Task<(string email, string name, string externalId)> GetGoogleUserInfo(string code, string redirectUri)
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

        var userData = await userInfoResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        var email = userData["email"]?.ToString();
        var name = userData["name"]?.ToString();
        var externalId = userData["id"]?.ToString();

        return (email, name, externalId);
    }
}
