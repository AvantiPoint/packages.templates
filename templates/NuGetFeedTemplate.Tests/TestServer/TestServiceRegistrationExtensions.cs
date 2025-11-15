using AvantiPoint.Packages.Core;
using AvantiPoint.Packages.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NuGetFeedTemplate.Authentication;
using NuGetFeedTemplate.Configuration;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Services;
using PostmarkDotNet;
using SendGrid;

namespace NuGetFeedTemplate.Tests.TestServer;

/// <summary>
/// Extension methods for registering test feed services.
/// This is a test-specific version that uses in-memory database.
/// </summary>
public static class TestServiceRegistrationExtensions
{
    public static NuGetApiOptions AddTestFeedServices(this NuGetApiOptions options)
    {
        options.Services.AddScoped<IPackageAuthenticationService, PackageAuthenticationService>();
        options.Services.AddScoped<INuGetFeedActionHandler, NuGetFeedActionHandler>();
        options.Services.AddScoped<ITemplateResourceProvider, LocalTemplateResourceProvider>();
        options.Services.AddScoped<ISyndicationService, SyndicationService>();

        options.Services
            .AddTransient<SendGridEmailService>()
            .AddTransient<PostmarkEmailService>()
            .AddTransient<NullEmailService>()
            .AddScoped<IEmailService>(x =>
            {
                var emailOptions = x.GetRequiredService<EmailSettings>();
                if (!string.IsNullOrEmpty(emailOptions.SendGridKey))
                    return x.GetRequiredService<SendGridEmailService>();
                else if (!string.IsNullOrEmpty(emailOptions.PostmarkKey))
                    return x.GetRequiredService<PostmarkEmailService>();

                return x.GetRequiredService<NullEmailService>();
            });

        options.Services.AddTransient<ISendGridClient>(x =>
        {
            var emailOptions = x.GetRequiredService<EmailSettings>();
            if (string.IsNullOrEmpty(emailOptions.SendGridKey))
                return null;

            return new SendGridClient(emailOptions.SendGridKey);
        });

        options.Services.AddTransient<PostmarkClient>(x =>
        {
            var emailOptions = x.GetRequiredService<EmailSettings>();
            if (string.IsNullOrEmpty(emailOptions.PostmarkKey))
                return null;

            return new PostmarkClient(emailOptions.PostmarkKey);
        });

        // Use in-memory database instead of SQL Server
        options.Services.AddDbContext<FeedContext>(o =>
        {
            o.UseInMemoryDatabase($"TestFeedDb_{Guid.NewGuid()}");
        });
        
        return options;
    }
}
