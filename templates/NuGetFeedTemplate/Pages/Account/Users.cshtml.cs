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
    public class UsersModel : PageModel
    {
        private FeedContext _dbContext { get; }

        public UsersModel(FeedContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IEnumerable<User> Users { get; set; }

        public async Task OnGet()
        {
            Users = await _dbContext.Users.ToArrayAsync();
        }

        public async Task OnPost([FromForm]User user)
        {
            var dbUser = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == user.Email);
            if(dbUser != null)
            {
                dbUser.PackagePublisher = user.PackagePublisher;
                _dbContext.Users.Update(dbUser);
                await _dbContext.SaveChangesAsync();
            }

            Users = _dbContext.Users.ToArray();
        }
    }
}
