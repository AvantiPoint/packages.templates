using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NuGetFeedTemplate.Configuration;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;
using NuGetFeedTemplate.Models;
using NuGetFeedTemplate.Services;
using SendGrid.Helpers.Mail;

namespace NuGetFeedTemplate.Pages
{
    [Authorize]
    public class CreateTokenModel : PageModel
    {
        private string _siteName { get; }

        public CreateTokenModel(
            FeedSettings settings)
        {
            _siteName = settings.ServerName;
            Token = new AuthToken();
        }

        public AuthToken Token { get; set; }

        public async Task OnPost(
            [Bind("Description")]AuthToken authToken,
            [FromServices]FeedContext dbContext,
            [FromServices]IEmailService emailService)
        {
            authToken.UserEmail = User.FindFirstValue("preferred_username");
            var to = new EmailAddress(authToken.UserEmail, User.FindFirstValue("name"));
            if (!await dbContext.Users.AnyAsync(x => x.Email == authToken.UserEmail))
            {
                var user = new User
                {
                    Email = authToken.UserEmail,
                    Name = User.FindFirstValue("name"),
                    PackagePublisher = !await dbContext.Users.AnyAsync()
                };
                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync();
                await emailService.SendEmail(
                    EmailTemplates.WelcomeUser,
                    to,
                    $"Welcome to {_siteName}",
                    new WelcomeMessage
                    {
                        Host = $"{Request.Scheme}://{Request.Host}",
                        SiteName = _siteName,
                        Username = user.Email
                    });
            }

            dbContext.AuthTokens.Add(authToken);
            await dbContext.SaveChangesAsync();
            await emailService.SendEmail(
                EmailTemplates.TokenCreated,
                to,
                "New Token Created",
                new TokenAction
                {
                    Description = authToken.Description,
                    Expires = authToken.Expires.ToString("F")
                });
            Token = authToken;
        }
    }
}
