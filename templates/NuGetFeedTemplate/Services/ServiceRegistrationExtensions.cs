using AvantiPoint.Packages.Core;
using AvantiPoint.Packages.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuGetFeedTemplate.Authentication;
using NuGetFeedTemplate.Configuration;
using NuGetFeedTemplate.Data;
using SendGrid;

namespace NuGetFeedTemplate.Services
{
    public static class ServiceRegistrationExtensions
    {
        public static NuGetApiOptions AddFeedServices(this NuGetApiOptions options)
        {
            options.Services.AddScoped<IPackageAuthenticationService, PackageAuthenticationService>();
            options.Services.AddScoped<INuGetFeedActionHandler, NuGetFeedActionHandler>();
            options.Services.AddScoped<IEmailService, EmailService>();
            options.Services.AddScoped<ITemplateResourceProvider, LocalTemplateResourceProvider>();

            options.Services.AddTransient<ISendGridClient>(x =>
            {
                var options = x.GetRequiredService<EmailSettings>();
                return new SendGridClient(options.SendGridKey);
            });

            options.Services.AddDbContext<FeedContext>(o =>
            {
                o.UseSqlServer(options.Configuration.GetConnectionString("DefaultConnection"));
            });
            return options;
        }
    }
}
