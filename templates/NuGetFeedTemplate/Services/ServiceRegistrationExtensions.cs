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
        public static IServiceCollection AddFeedServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IPackageAuthenticationService, PackageAuthenticationService>();
            services.AddScoped<INuGetFeedActionHandler, NuGetFeedActionHandler>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITemplateResourceProvider, LocalTemplateResourceProvider>();

            services.AddTransient<ISendGridClient>(x =>
            {
                var options = x.GetRequiredService<EmailSettings>();
                return new SendGridClient(options.SendGridKey);
            });

            services.AddDbContext<FeedContext>(o =>
            {
                o.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });
            return services;
        }
    }
}
