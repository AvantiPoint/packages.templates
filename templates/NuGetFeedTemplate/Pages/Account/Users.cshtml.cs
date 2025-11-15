using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;
using NuGetFeedTemplate.Models;
using NuGetFeedTemplate.Services;

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

        public async Task OnPost([FromForm]User user, [FromServices] IEmailService emailService)
        {
            var dbUser = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == user.Email);
            if(dbUser != null)
            {
                // Handle PackagePublisher change
                if (dbUser.PackagePublisher != user.PackagePublisher)
                {
                    dbUser.PackagePublisher = user.PackagePublisher;
                }
                
                // Handle IsRevoked change
                if (dbUser.IsRevoked != user.IsRevoked)
                {
                    dbUser.IsRevoked = user.IsRevoked;
                    
                    // Send email notification
                    var to = new MailAddress(dbUser.Email, dbUser.Name);
                    var adminName = User.FindFirstValue("name");
                    var ipAddress = HttpContext.Connection.RemoteIpAddress.ToString();
                    
                    if (dbUser.IsRevoked)
                    {
                        await emailService.SendEmail(
                            EmailTemplates.UserRevoked,
                            to,
                            "Account Access Revoked",
                            new UserAction
                            {
                                RevokedBy = adminName,
                                IPAddress = ipAddress
                            });
                    }
                    else
                    {
                        await emailService.SendEmail(
                            EmailTemplates.UserRestored,
                            to,
                            "Account Access Restored",
                            new UserAction
                            {
                                RestoredBy = adminName,
                                IPAddress = ipAddress
                            });
                    }
                }
                
                _dbContext.Users.Update(dbUser);
                await _dbContext.SaveChangesAsync();
            }

            Users = _dbContext.Users.ToArray();
        }
    }
}
