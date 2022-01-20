using NuGetFeedTemplate.Configuration;

namespace NuGetFeedTemplate.Services;

public class LocalTemplateResourceProvider : ITemplateResourceProvider
{
    private ILogger _logger { get; }
    private IWebHostEnvironment _env { get; }
    private string _templateDirectory { get; }

    public LocalTemplateResourceProvider(
        EmailSettings settings,
        IWebHostEnvironment env,
        ILogger<LocalTemplateResourceProvider> logger)
    {
        _env = env;
        _logger = logger;
        _templateDirectory = settings.TemplatesDirectory;
    }

    public string ReadFile(string fileName)
    {
        var path = Path.Combine(_env.WebRootPath, _templateDirectory, fileName);
        return File.ReadAllText(path);
    }
}
