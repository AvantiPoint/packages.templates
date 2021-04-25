using AvantiPoint.Packages.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace NuGetFeedTemplate.Configuration
{
    public static class FeedConfigurationExtensions
    {
        public static NuGetApiOptions AddFeedConfiguration(this NuGetApiOptions options)
        {
            options.Services.Configure<FeedSettings>(options.Configuration.GetSection(nameof(FeedSettings)));
            options.Services.AddTransient(sp => sp.GetRequiredService<IOptionsSnapshot<FeedSettings>>().Value);

            options.Services.Configure<EmailSettings>(options.Configuration.GetSection(nameof(EmailSettings)));
            options.Services.AddTransient(sp => sp.GetRequiredService<IOptionsSnapshot<EmailSettings>>().Value);

            return options;
        }
    }
}
