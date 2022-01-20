using AvantiPoint.Packages.Core;
using Microsoft.Extensions.Options;

namespace NuGetFeedTemplate.Configuration;

public static class FeedConfigurationExtensions
{
    public static NuGetApiOptions AddFeedConfiguration(this NuGetApiOptions options)
    {
        options.Services.Configure<FeedSettings>(options.Configuration.GetSection(nameof(FeedSettings)));
        options.Services.AddTransient(sp =>
        {
            var settings = sp.GetRequiredService<IOptionsSnapshot<FeedSettings>>().Value ?? new FeedSettings();
            if (string.IsNullOrEmpty(settings.ServerName))
                settings.ServerName = "Server Name not Configured";
            return settings;
        });

        options.Services.Configure<EmailSettings>(options.Configuration.GetSection(nameof(EmailSettings)));
        options.Services.AddTransient(sp => sp.GetRequiredService<IOptionsSnapshot<EmailSettings>>().Value);

        return options;
    }
}
