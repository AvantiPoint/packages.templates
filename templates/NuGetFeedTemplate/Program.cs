using System.Security.Claims;
using System.Text;
using AvantiPoint.Packages;
using AvantiPoint.Packages.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NuGetFeedTemplate.Authentication;
using NuGetFeedTemplate.Configuration;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;
using NuGetFeedTemplate.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure JWT settings
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection("JwtSettings").Bind(jwtSettings);
builder.Services.AddSingleton(jwtSettings);

// Configure OAuth settings
var oauthSettings = new OAuthSettings();
builder.Configuration.GetSection("OAuth").Bind(oauthSettings);
builder.Services.AddSingleton(oauthSettings);

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
builder.Services.AddAuthentication(options =>
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

builder.Services.AddHttpClient();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy
    options.FallbackPolicy = options.DefaultPolicy;
});

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