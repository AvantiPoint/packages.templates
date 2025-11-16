using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGetFeedTemplate.Configuration;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;
using NuGetFeedTemplate.Models;
using NuGetFeedTemplate.Services;
using System.Security.Claims;
using System.Text;

namespace NuGetFeedTemplate.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthenticationController : ControllerBase
{
    private readonly FeedContext _dbContext;
    private readonly IJwtTokenService _tokenService;
    private readonly OAuthSettings _oauthSettings;
    private readonly ILogger<AuthenticationController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthenticationController(
        FeedContext dbContext,
        IJwtTokenService tokenService,
        OAuthSettings oauthSettings,
        ILogger<AuthenticationController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _oauthSettings = oauthSettings;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("login/{provider}")]
    [AllowAnonymous]
    public IActionResult Login(string provider)
    {
        if (!IsValidProvider(provider))
            return BadRequest(new { error = "Invalid provider" });

        var redirectUri = GetRedirectUri(provider);
        var authUrl = provider.ToLower() == "microsoft" 
            ? GetMicrosoftAuthUrl(redirectUri)
            : GetGoogleAuthUrl(redirectUri);

        return Ok(new { authUrl });
    }

    [HttpGet("callback/{provider}")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(string provider, [FromQuery] string code, [FromQuery] string error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("OAuth error: {Error}", error);
            return Redirect($"/?error={Uri.EscapeDataString(error)}");
        }

        if (string.IsNullOrEmpty(code))
        {
            return Redirect("/?error=no_code");
        }

        try
        {
            var redirectUri = GetRedirectUri(provider);
            var (email, name, externalId) = await GetUserInfoFromProvider(provider, code, redirectUri);

            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email);
            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    Name = name,
                    ExternalProvider = provider,
                    ExternalId = externalId,
                    PackagePublisher = !await _dbContext.Users.AnyAsync()
                };
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                // Update external provider info if changed
                user.ExternalProvider = provider;
                user.ExternalId = externalId;
                user.LastLoginAt = DateTimeOffset.Now;
                await _dbContext.SaveChangesAsync();
            }

            if (user.IsRevoked)
            {
                return Redirect("/?error=user_revoked");
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var (accessToken, refreshToken) = await _tokenService.GenerateTokensAsync(user, ipAddress);

            // Create or update system token
            var systemToken = await _dbContext.AuthTokens
                .FirstOrDefaultAsync(x => x.UserEmail == email && x.IsSystemToken && !x.Revoked && x.Expires > DateTimeOffset.Now);

            if (systemToken == null)
            {
                systemToken = new AuthToken
                {
                    Description = "System Token",
                    UserEmail = email,
                    IsSystemToken = true,
                    Expires = DateTimeOffset.Now.AddHours(24)
                };
                _dbContext.AuthTokens.Add(systemToken);
                await _dbContext.SaveChangesAsync();
            }

            // Redirect to a page that will handle setting cookies and local storage
            var callbackUrl = $"/auth-callback?access_token={Uri.EscapeDataString(accessToken)}&refresh_token={Uri.EscapeDataString(refreshToken)}&email={Uri.EscapeDataString(email)}&name={Uri.EscapeDataString(name)}&is_admin={user.PackagePublisher}";
            return Redirect(callbackUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OAuth callback");
            return Redirect($"/?error={Uri.EscapeDataString("authentication_failed")}");
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _tokenService.RefreshTokenAsync(request.RefreshToken, ipAddress);

        if (result == null)
            return Unauthorized(new { error = "Invalid refresh token" });

        var (accessToken, refreshToken) = result.Value;
        return Ok(new { accessToken, refreshToken });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        if (!string.IsNullOrEmpty(request?.RefreshToken))
        {
            await _tokenService.RevokeTokenAsync(request.RefreshToken, ipAddress);
        }

        if (!string.IsNullOrEmpty(email))
        {
            await _tokenService.RevokeAllUserTokensAsync(email, ipAddress);
        }

        return Ok(new { message = "Logged out successfully" });
    }

    private bool IsValidProvider(string provider)
    {
        return provider?.ToLower() == "microsoft" || provider?.ToLower() == "google";
    }

    private string GetRedirectUri(string provider)
    {
        var scheme = HttpContext.Request.Scheme;
        var host = HttpContext.Request.Host.ToString();
        var callbackPath = provider.ToLower() == "microsoft" 
            ? _oauthSettings.Microsoft?.CallbackPath ?? "/api/authentication/callback/microsoft"
            : _oauthSettings.Google?.CallbackPath ?? "/api/authentication/callback/google";
        
        return $"{scheme}://{host}{callbackPath}";
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

    private async Task<(string email, string name, string externalId)> GetUserInfoFromProvider(string provider, string code, string redirectUri)
    {
        if (provider.ToLower() == "microsoft")
            return await GetMicrosoftUserInfo(code, redirectUri);
        else
            return await GetGoogleUserInfo(code, redirectUri);
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
        userInfoRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        
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
        userInfoRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        
        var userInfoResponse = await httpClient.SendAsync(userInfoRequest);
        userInfoResponse.EnsureSuccessStatusCode();
        
        var userData = await userInfoResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        
        var email = userData["email"]?.ToString();
        var name = userData["name"]?.ToString();
        var externalId = userData["id"]?.ToString();

        return (email, name, externalId);
    }
}
