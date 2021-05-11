using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;

namespace NuGetFeedTemplate.Pages.Account
{
    [Authorize(Roles = "Admin")]
    public class PackageGroupsModel : PageModel
    {
        private FeedContext _db { get; }

        public PackageGroupsModel(FeedContext db)
        {
            _db = db;
        }

        public IEnumerable<PackageGroup> PackageGroups { get; set; }

        public IEnumerable<PublishTarget> PublishTargets { get; set; }

        public async Task OnGet()
        {
            PackageGroups = await _db.PackageGroups
                .Include(x => x.Members)
                .Include(x => x.Syndications)
                .ToArrayAsync();

            PublishTargets = await _db.PublishTargets.ToArrayAsync();
        }

        public async Task OnPost([Bind(nameof(PackageGroup.Name))]PackageGroup group)
        {
            if(await _db.PackageGroups.AnyAsync(x => x.Name == group.Name))
            {
                ModelState.AddModelError("Name", $"A package group already exists with the name '{group.Name}'.");
            }
            else
            {
                _db.PackageGroups.Add(group);
                await _db.SaveChangesAsync();
            }

            PackageGroups = await _db.PackageGroups
                .Include(x => x.Members)
                .Include(x => x.Syndications)
                .ToArrayAsync();

            PublishTargets = await _db.PublishTargets.ToArrayAsync();
        }
    }
}
