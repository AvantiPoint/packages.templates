using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AvantiPoint.Packages.Core;
using AvantiPoint.Packages.Protocol;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;

namespace NuGetFeedTemplate.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private FeedContext _dbContext { get; }
        private IContext _context { get; }

        public AccountController(FeedContext dbContext, IContext context)
        {
            _dbContext = dbContext;
            _context = context;
        }

        [HttpPost("package-groups/add/package")]
        public async Task<IActionResult> AddPackageToGroup([FromForm]PackageGroupAssociation groupAssociation)
        {
            if(string.IsNullOrEmpty(groupAssociation.Group) || string.IsNullOrEmpty(groupAssociation.PackageId))
                return PackageGroups();

            var group = await _dbContext.PackageGroups
                .Include(x => x.Members)
                .Include(x => x.Syndications)
                .FirstOrDefaultAsync(x => x.Name == groupAssociation.Group);

            if (group is null || group.Members.Any(x => x.PackageId == groupAssociation.PackageId))
                return PackageGroups();

            if (!await _context.Packages.AnyAsync(x => x.Id == groupAssociation.PackageId))
                return PackageGroups();

            _dbContext.PackageGroupMembers.Add(new PackageGroupMember
            {
                PackageGroupName = group.Name,
                PackageId = groupAssociation.PackageId
            });
            await _dbContext.SaveChangesAsync();

            return PackageGroups();
        }

        [HttpPost("package-groups/add/syndication")]
        public async Task<IActionResult> AddSyndicationFeed([FromForm] PackageGroupAssociation groupAssociation)
        {
            if (string.IsNullOrEmpty(groupAssociation.Group) || string.IsNullOrEmpty(groupAssociation.Feed))
                return PackageGroups();

            var group = await _dbContext.PackageGroups
                .Include(x => x.Members)
                .Include(x => x.Syndications)
                .FirstOrDefaultAsync(x => x.Name == groupAssociation.Group);

            if (group is null || group.Syndications.Any(x => x.PublishTargetName == groupAssociation.Feed))
                return PackageGroups();

            _dbContext.Syndications.Add(new PackageGroupSyndication
            {
                PackageGroupName = group.Name,
                PublishTargetName = groupAssociation.Feed
            });
            await _dbContext.SaveChangesAsync();

            return PackageGroups();
        }

        [HttpPost("package-groups/{groupName}/push-latest")]
        public async Task<IActionResult> PushLatest(string groupName, [FromForm] PackageGroupAssociation groupAssociation,
            [FromServices]IPackageStorageService packageStorage, [FromServices]ISymbolStorageService symbolStorage)
        {
            if (groupName != groupAssociation.Group)
                return PackageGroups();

            var group = await _dbContext.PackageGroups
                .Include(x => x.Members)
                .Include(x => x.Syndications)
                    .ThenInclude(x => x.PublishTarget)
                .FirstOrDefaultAsync(x => x.Name == groupName);

            if (group is null)
                return PackageGroups();

            var targets = group.Syndications.Select(x => x.PublishTarget);
            foreach(var package in group.Members)
            {
                var nugetPackages = await _context.Packages.Where(x => x.Id == package.PackageId && x.Listed == true)
                    .ToArrayAsync();

                var nugetPackage = nugetPackages
                    .OrderByDescending(x => x.Version)
                    .FirstOrDefault();

                if (nugetPackage is null)
                    continue;

                foreach(var targetFeed in targets)
                {
                    var client = new NuGetClient(targetFeed.PublishEndpoint.ToString());
                    await client.UploadPackageAsync(nugetPackage.Id, nugetPackage.Version, targetFeed.ApiToken, packageStorage);
                    await client.UploadSymbolsPackageAsync(nugetPackage.Id, nugetPackage.Version, targetFeed.ApiToken, symbolStorage);
                }
            }

            // TODO: Add Success Page
            return PackageGroups();
        }

        [HttpGet("syndication-target/{targetName}/delete")]
        public async Task<IActionResult> DeleteTarget(string targetName)
        {
            var target = await _dbContext.PublishTargets.FirstOrDefaultAsync(x => x.Name == targetName);
            if(target != null)
            {
                _dbContext.PublishTargets.Remove(target);
                await _dbContext.SaveChangesAsync();
            }

            return LocalRedirect("/account/publish-targets");
        }

        [HttpGet("packageGroups/{groupName}/remove/{packageId}")]
        public async Task<IActionResult> RemovePackageFromGroup(string groupName, string packageId)
        {
            var group = await _dbContext.PackageGroups
                .Include(x => x.Members)
                .FirstOrDefaultAsync(x => x.Name == groupName);

            if(group != null && group.Members.Any(x => x.PackageId == packageId))
            {
                var member = group.Members.First(x => x.PackageId == packageId);
                _dbContext.PackageGroupMembers.Remove(member);
                await _dbContext.SaveChangesAsync();
            }

            return PackageGroups();
        }

        [HttpGet("packageGroups/{groupName}/syndication/{targetName}/remove")]
        public async Task<IActionResult> RemoveSyndicationFromGroup(string groupName, string targetName)
        {
            var group = await _dbContext.PackageGroups
               .Include(x => x.Syndications)
               .FirstOrDefaultAsync(x => x.Name == groupName);

            if(group != null && group.Syndications.Any(x => x.PublishTargetName == targetName))
            {
                var syndication = group.Syndications.First(x => x.PublishTargetName == targetName);
                _dbContext.Syndications.Remove(syndication);
                await _dbContext.SaveChangesAsync();
            }

            return PackageGroups();
        }

        private IActionResult PackageGroups()
        {
            return LocalRedirect("/account/package-groups");
        }

        public class PackageGroupAssociation
        {
            public string PackageId { get; set; }

            public string Feed { get; set; }

            public string Group { get; set; }
        }
    }
}
