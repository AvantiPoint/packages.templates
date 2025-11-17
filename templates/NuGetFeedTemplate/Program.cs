using System.Security.Claims;
using AvantiPoint.Packages;
using AvantiPoint.Packages.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using NuGetFeedTemplate.Authentication;
using NuGetFeedTemplate.Configuration;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;
using NuGetFeedTemplate.Models;
using NuGetFeedTemplate.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNuGetPackageApi(options =>
{
    switch (options.Options.Storage.Type)
    {
        case "AzureBlobStorage":
            options.AddAzureBlobStorage();
            break;
        default:
            options.AddFileStorage();
            break;
    }

    options.AddFeedConfiguration()
       .AddFeedServices()
       .AddSqlServerDatabase("DefaultConnection");
});

// Add JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddHttpClient();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IOAuthService, OAuthService>();

builder.Services.AddRazorPages();
builder.Services.AddControllers();

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue;
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = int.MaxValue; // if don't set default value is: 30 MB
});

builder.Services.Configure<FormOptions>(x =>
{
    x.ValueLengthLimit = int.MaxValue;
    x.MultipartBodyLengthLimit = int.MaxValue; // if don't set default value is: 128 MB
    x.MultipartHeadersLengthLimit = int.MaxValue;
});

var app = builder.Build();
try
{
    await app.InitializeDatabaseContext();
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseOperationCancelledMiddleware();

    app.UseAuthentication();
    app.UseAuthorization();

    // Authentication Minimal API Endpoints
    var authGroup = app.MapGroup("/api/authentication");

    authGroup.MapGet("/login/{provider}", async (string provider, IOAuthService oauthService, HttpContext httpContext) =>
    {
        if (!oauthService.IsValidProvider(provider))
            return Results.BadRequest(new { error = "Invalid provider" });

        var redirectUri = oauthService.GetRedirectUri(httpContext, provider);
        var authUrl = oauthService.GetAuthUrl(provider, redirectUri);

        return Results.Ok(new { authUrl });
    })
    .AllowAnonymous();

    authGroup.MapGet("/callback/{provider}", async (
        string provider,
        string code,
        string error,
        IOAuthService oauthService,
        FeedContext dbContext,
        IJwtTokenService tokenService,
        ILogger<Program> logger,
        HttpContext httpContext) =>
    {
        if (!string.IsNullOrEmpty(error))
        {
            logger.LogWarning("OAuth error: {Error}", error);
            return Results.Redirect($"/?error={Uri.EscapeDataString(error)}");
        }

        if (string.IsNullOrEmpty(code))
        {
            return Results.Redirect("/?error=no_code");
        }

        try
        {
            var redirectUri = oauthService.GetRedirectUri(httpContext, provider);
            var userInfo = await oauthService.GetUserInfoFromProvider(provider, code, redirectUri);

            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == userInfo.Email);
            if (user == null)
            {
                user = new User
                {
                    Email = userInfo.Email,
                    Name = userInfo.DisplayName,
                    FirstName = userInfo.FirstName,
                    LastName = userInfo.LastName,
                    ProfilePictureUrl = userInfo.ProfilePictureUrl,
                    PackagePublisher = !await dbContext.Users.AnyAsync()
                };
                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync();
            }
            else
            {
                // Update user info on login
                user.FirstName = userInfo.FirstName;
                user.LastName = userInfo.LastName;
                user.ProfilePictureUrl = userInfo.ProfilePictureUrl;
                user.LastLoginAt = DateTimeOffset.Now;
                await dbContext.SaveChangesAsync();
            }

            if (user.IsRevoked)
            {
                return Results.Redirect("/?error=user_revoked");
            }

            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            var (accessToken, refreshToken) = await tokenService.GenerateTokensAsync(user, ipAddress, userAgent);

            // Create or update system token
            var systemToken = await dbContext.AuthTokens
                .FirstOrDefaultAsync(x => x.UserEmail == userInfo.Email && x.IsSystemToken && !x.Revoked && x.Expires > DateTimeOffset.Now);

            if (systemToken == null)
            {
                systemToken = new AuthToken
                {
                    Description = "System Token",
                    UserEmail = userInfo.Email,
                    IsSystemToken = true,
                    Expires = DateTimeOffset.Now.AddHours(24)
                };
                dbContext.AuthTokens.Add(systemToken);
                await dbContext.SaveChangesAsync();
            }

            // Redirect to a page that will handle setting cookies and local storage
            var callbackUrl = $"/auth-callback?access_token={Uri.EscapeDataString(accessToken)}&refresh_token={Uri.EscapeDataString(refreshToken)}&email={Uri.EscapeDataString(userInfo.Email)}&name={Uri.EscapeDataString(userInfo.DisplayName)}&is_admin={user.PackagePublisher}";
            return Results.Redirect(callbackUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during OAuth callback");
            return Results.Redirect($"/?error={Uri.EscapeDataString("authentication_failed")}");
        }
    })
    .AllowAnonymous();

    authGroup.MapPost("/refresh", async (RefreshTokenRequest request, IJwtTokenService tokenService, HttpContext httpContext) =>
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        var result = await tokenService.RefreshTokenAsync(request.RefreshToken, ipAddress, userAgent);

        if (result == null)
            return Results.Unauthorized();

        var (accessToken, refreshToken) = result.Value;
        return Results.Ok(new { accessToken, refreshToken });
    })
    .AllowAnonymous();

    authGroup.MapPost("/logout", async (
        RefreshTokenRequest request,
        IJwtTokenService tokenService,
        HttpContext httpContext) =>
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var email = httpContext.User.FindFirst(ClaimTypes.Email)?.Value;

        if (!string.IsNullOrEmpty(request?.RefreshToken))
        {
            await tokenService.RevokeTokenAsync(request.RefreshToken, ipAddress);
        }

        if (!string.IsNullOrEmpty(email))
        {
            await tokenService.RevokeAllUserTokensAsync(email, ipAddress);
        }

        return Results.Ok(new { message = "Logged out successfully" });
    })
    .RequireAuthorization();

    app.MapRazorPages();
    app.MapControllers();
    app.MapNuGetApiRoutes();
    await app.RunAsync();
}
catch (Exception ex)
{
    var logFactory = app.Services.GetService<ILoggerFactory>();
    var logger = logFactory.CreateLogger("Program");
    logger.LogError(ex,"An unexpected error occurred.");
}