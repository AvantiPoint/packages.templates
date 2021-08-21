using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
    public class PublishTargetsModel : PageModel
    {
        private FeedContext _db { get; }

        public PublishTargetsModel(FeedContext db)
        {
            _db = db;

            Target = new PublishTarget();
        }

        public PublishTarget Target { get; set; }

        public IEnumerable<PublishTarget> PublishTargets { get; set; }

        public async Task OnGet([FromQuery(Name = "edit")]string targetName = null)
        {
            Target = new PublishTarget();
            PublishTargets = await _db.PublishTargets.ToArrayAsync();

            if(!string.IsNullOrEmpty(targetName) && PublishTargets.Any(x => x.Name == targetName))
            {
                Target = PublishTargets.First(x => x.Name == targetName);
            }
        }

        public async Task OnPost([FromForm]PublishTarget target)
        {
            Target = new PublishTarget();
            var existingTarget = await _db.PublishTargets.FirstOrDefaultAsync(x => x.Name == target.Name);

            if(existingTarget is null)
            {
                target.AddedBy = User.FindFirstValue("name");
                _db.PublishTargets.Add(target);
            }
            else
            {
                existingTarget.PublishEndpoint = target.PublishEndpoint;
                existingTarget.Legacy = target.Legacy;
                existingTarget.ApiToken = target.ApiToken;
                existingTarget.Timestamp = DateTimeOffset.Now;
                _db.PublishTargets.Update(existingTarget);
            }

            await _db.SaveChangesAsync();

            PublishTargets = await _db.PublishTargets.ToArrayAsync();
        }
    }
}
