using AvantiPoint.Packages.Core;
using AvantiPoint.Packages.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuGetFeedTemplate.Authentication;
using NuGetFeedTemplate.Configuration;
using NuGetFeedTemplate.Data;
using PostmarkDotNet;
using SendGrid;

namespace NuGetFeedTemplate.Services
{
    public static class ServiceRegistrationExtensions
    {
        public static NuGetApiOptions AddFeedServices(this NuGetApiOptions options)
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
                var options = x.GetRequiredService<EmailSettings>();
                if (!string.IsNullOrEmpty(options.SendGridKey))
                    return x.GetRequiredService<SendGridEmailService>();
                else if (!string.IsNullOrEmpty(options.PostmarkKey))
                    return x.GetRequiredService<PostmarkEmailService>();

                return x.GetRequiredService<NullEmailService>();
            });

            options.Services.AddTransient<ISendGridClient>(x =>
            {
                var options = x.GetRequiredService<EmailSettings>();
                if (string.IsNullOrEmpty(options.SendGridKey))
                    return null;

                return new SendGridClient(options.SendGridKey);
            });

            options.Services.AddTransient<PostmarkClient>(x =>
            {
                var options = x.GetRequiredService<EmailSettings>();
                if (string.IsNullOrEmpty(options.PostmarkKey))
                    return null;

                return new PostmarkClient(options.PostmarkKey);
            });

            options.Services.AddDbContext<FeedContext>(o =>
            {
                o.UseSqlServer(options.Configuration.GetConnectionString("DefaultConnection"));
            });
            return options;
        }
    }
}
