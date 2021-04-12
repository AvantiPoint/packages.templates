using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace NuGetFeedTemplate.Configuration
{
    public static class FeedConfigurationExtensions
    {
        public static IServiceCollection AddFeedConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FeedSettings>(configuration.GetSection(nameof(FeedSettings)));
            services.AddTransient(sp => sp.GetRequiredService<IOptionsSnapshot<FeedSettings>>().Value);

            services.Configure<EmailSettings>(configuration.GetSection(nameof(EmailSettings)));
            services.AddTransient(sp => sp.GetRequiredService<IOptionsSnapshot<EmailSettings>>().Value);

            return services;
        }
    }
}
