using NuGet.Versioning;

namespace NuGetFeedTemplate.Services;

public interface ISyndicationService
{
    Task PushToSource(string groupName, string targetName);
    Task SyndicatePackage(string packageId, NuGetVersion version);
    Task SyndicateSymbols(string packageId, NuGetVersion version);
}
