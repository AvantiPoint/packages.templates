using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NuGetFeedTemplate.Configuration;
using NuGetFeedTemplate.Data;
using System.Security.Claims;

namespace NuGetFeedTemplate.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure JWT settings
        var jwtSettings = new JwtSettings();
        configuration.GetSection("JwtSettings").Bind(jwtSettings);
        services.AddSingleton(jwtSettings);

        // Configure OAuth settings with backward compatibility
        var oauthSettings = new OAuthSettings();
        configuration.GetSection("OAuth").Bind(oauthSettings);

        // Support legacy AzureAd configuration for backward compatibility
        var azureAdSection = configuration.GetSection("AzureAd");
        if (azureAdSection.Exists() && oauthSettings.Microsoft == null)
        {
            oauthSettings.Microsoft = new MicrosoftOAuthSettings();
            oauthSettings.Microsoft.TenantId = azureAdSection["TenantId"];
            oauthSettings.Microsoft.ClientId = azureAdSection["ClientId"];
            oauthSettings.Microsoft.ClientSecret = azureAdSection["ClientSecret"];
            oauthSettings.Microsoft.Instance = azureAdSection["Instance"] ?? "https://login.microsoftonline.com/";
            oauthSettings.Microsoft.CallbackPath = azureAdSection["CallbackPath"] ?? "/api/authentication/callback/microsoft";
            oauthSettings.Provider = "Microsoft";
        }

        services.AddSingleton(oauthSettings);

        // Add JWT Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
            };

            // Support token from cookie for browser requests
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Check for token in cookie first (for browser)
                    if (context.Request.Cookies.TryGetValue("access_token", out var token))
                    {
                        context.Token = token;
                    }
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var feedContext = context.HttpContext.RequestServices.GetRequiredService<FeedContext>();
                    var email = context.Principal.FindFirstValue(ClaimTypes.Email);

                    if (!string.IsNullOrEmpty(email))
                    {
                        var user = await feedContext.Users.FirstOrDefaultAsync(x => x.Email == email);

                        if (user != null && user.IsRevoked)
                        {
                            context.Fail("User access has been revoked.");
                        }
                    }
                }
            };
        });

        services.AddAuthorization(options =>
        {
            // By default, all incoming requests will be authorized according to the default policy
            options.FallbackPolicy = options.DefaultPolicy;
        });

        return services;
    }
}
