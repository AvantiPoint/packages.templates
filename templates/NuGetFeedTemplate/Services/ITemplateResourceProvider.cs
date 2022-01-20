namespace NuGetFeedTemplate.Services;

public interface ITemplateResourceProvider
{
    string ReadFile(string fileName);
}
