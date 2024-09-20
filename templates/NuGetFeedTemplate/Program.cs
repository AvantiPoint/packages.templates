using System.Security.Claims;
using AvantiPoint.Packages;
using AvantiPoint.Packages.Hosting;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using NuGetFeedTemplate.Configuration;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;
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

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = OnTokenValidated
    };
});

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy
    options.FallbackPolicy = options.DefaultPolicy;
});
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

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

static async Task OnTokenValidated(TokenValidatedContext ctx)
{
    var feedContext = ctx.HttpContext.RequestServices.GetRequiredService<FeedContext>();
    var email = ctx.Principal.FindFirstValue("preferred_username");
    var user = await feedContext.Users.FirstOrDefaultAsync(x => x.Email == email);
    if (user is null)
    {
        user = new User
        {
            Email = email,
            Name = ctx.Principal.FindFirstValue("name"),
            PackagePublisher = !await feedContext.Users.AnyAsync()
        };
        feedContext.Users.Add(user);
        await feedContext.SaveChangesAsync();
    }

    if (user.PackagePublisher)
    {
        var claimsIdentity = ctx.Principal.Identity as ClaimsIdentity;
        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
    }
}