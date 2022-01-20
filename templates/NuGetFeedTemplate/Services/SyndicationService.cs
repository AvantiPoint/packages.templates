using AvantiPoint.Packages.Core;
using AvantiPoint.Packages.Protocol;
using Microsoft.EntityFrameworkCore;
using NuGet.Versioning;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;

namespace NuGetFeedTemplate.Services;

public class SyndicationService : ISyndicationService
{
    private FeedContext _feedContext { get; }
    private IContext _packageContext { get; }
    private IPackageStorageService _packageStorageService { get; }
    private ISymbolStorageService _symbolStorageService { get; }

    public SyndicationService(
        FeedContext feedContext,
        IContext packageContext,
        IPackageStorageService packageStorageService,
        ISymbolStorageService symbolStorageService)
    {
        _feedContext = feedContext;
        _packageContext = packageContext;
        _packageStorageService = packageStorageService;
        _symbolStorageService = symbolStorageService;
    }

    public async Task SyndicatePackage(string packageId, NuGetVersion version)
    {
        var targets = await TargetLookup(packageId);

        foreach (var target in targets)
            await PushPackageToSource(packageId, version, target);
    }

    public async Task SyndicateSymbols(string packageId, NuGetVersion version)
    {
        var targets = await TargetLookup(packageId);

        foreach (var target in targets)
            await PushSymbolsToSource(packageId, version, target);
    }

    private async Task<IEnumerable<PublishTarget>> TargetLookup(string packageId)
    {
        var groups = await _feedContext.PackageGroups
            .Include(x => x.Members)
            .Include(x => x.Syndications)
            .ThenInclude(x => x.PublishTarget)
            .Where(x => x.Members.Any(m => m.PackageId == packageId))
            .ToListAsync();

        if (!groups.Any())
            return Array.Empty<PublishTarget>();

        return groups.SelectMany(x => x.Syndications)
            .Select(x => x.PublishTarget)
            .Distinct();
    }

    public async Task PushToSource(string groupName, string targetName)
    {
        var group = await _feedContext.PackageGroups
            .Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Name == groupName);

        if (group is null)
            return;

        var target = await _feedContext.PublishTargets.FirstOrDefaultAsync(x => x.Name == targetName);

        if (target is null)
            return;

        foreach (var member in group.Members)
        {
            var package = await _packageContext.Packages
                .Where(x => x.Id == member.PackageId)
                .OrderByDescending(x => x.Version)
                .FirstOrDefaultAsync();

            if (package is null)
                continue;

            await PushPackageToSource(package.Id, package.Version, target);
            await PushSymbolsToSource(package.Id, package.Version, target);
        }
    }

    private async Task<bool> PushPackageToSource(string packageId, NuGetVersion packageVersion, PublishTarget target)
    {
        var client = new NuGetClient(target.PublishEndpoint.ToString());
        return await client.UploadPackageAsync(packageId, packageVersion, target.ApiToken, _packageStorageService);
    }

    private async Task<bool> PushSymbolsToSource(string packageId, NuGetVersion packageVersion, PublishTarget target)
    {
        var client = new NuGetClient(target.PublishEndpoint.ToString());
        return await client.UploadSymbolsPackageAsync(packageId, packageVersion, target.ApiToken, _symbolStorageService);
    }
}
